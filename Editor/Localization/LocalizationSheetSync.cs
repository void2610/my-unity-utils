using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine.Localization.Tables;

namespace Void2610.UnityTemplate.Editor.Localization
{
    /// <summary>
    /// StringTable と Google スプレッドシートの差分確認・書き戻し・取り込みを、ダイアログを出さずに行う
    /// </summary>
    public static class LocalizationSheetSync
    {
        // 前回の同期時点の値を置く隠しシート。これと比べてローカルと Drive のどちらで変わったかを決める
        private const string BASE_SHEET_TITLE = "_LocalizationSync";

        private sealed class SheetTarget
        {
            public List<string> Locales => LocaleColumns.Select(c => c.Locale).ToList();
            public StringTableCollection Collection;
            public GoogleSheetsExtension Extension;
            public string TableName;
            public string Title;
            public int SheetId;
            public int GridRowCount;
            public int KeyColumnIndex;
            public List<(string Locale, int ColumnIndex)> LocaleColumns;
        }

        private sealed class SpreadsheetData
        {
            public SheetsService Service;
            public string SpreadsheetId;
            public List<SheetTarget> Targets;
            public Dictionary<string, DriveSheet> Drives;
            public SheetProperties BaseSheet;
            public int BaseRowCount;
            public Dictionary<string, TranslationRows> BaseTables;
        }

        private sealed class SheetWrites
        {
            public List<Request> Structure { get; } = new();
            public List<ValueRange> Values { get; } = new();
            public List<Request> Deletes { get; } = new();
            public ValueRange Base { get; set; }
        }

        private sealed class DriveSheet
        {
            public TranslationRows Rows;
            public Dictionary<string, int> RowIndexByKey;
            public int RowCount;
            public List<string> DuplicateKeys;
        }

        public static string FindSpreadsheetId() => LinkedCollections("").Select(c => c.Extensions.OfType<GoogleSheetsExtension>().First().SpreadsheetId).FirstOrDefault();

        public static string Diff(string tableFilter, string spreadsheetIdOverride)
        {
            var report = new StringBuilder();
            foreach (var spreadsheet in Load(tableFilter, spreadsheetIdOverride))
            {
                foreach (var target in spreadsheet.Targets)
                {
                    var drive = spreadsheet.Drives[target.TableName];
                    if (AppendDuplicateError(report, target, drive)) continue;

                    var cells = SheetMergePlanner.Compare(target.Locales, ReadLocal(target), drive.Rows, BaseOf(spreadsheet.BaseTables, target.TableName));
                    var counts = Enum.GetValues(typeof(SheetCellState)).Cast<SheetCellState>()
                        .Select(s => $"{SheetMergePlanner.Describe(s)} {cells.Count(c => c.State == s)}");
                    report.AppendLine($"[{target.TableName}] {string.Join(" / ", counts)}");
                    foreach (var cell in cells.Where(c => c.State != SheetCellState.InSync))
                        AppendCell(report, SheetMergePlanner.Describe(cell.State), cell);
                }
            }
            return report.ToString();
        }

        /// <summary>
        /// ローカルで変わったセルだけを Drive へ書く。Drive 側で変わったセルと衝突したセルは書かずに報告する
        /// </summary>
        public static string Push(string tableFilter, ICollection<string> forceKeys, string spreadsheetIdOverride)
        {
            var report = new StringBuilder();
            foreach (var spreadsheet in Load(tableFilter, spreadsheetIdOverride))
            {
                var writes = new SheetWrites();
                foreach (var target in spreadsheet.Targets)
                {
                    var drive = spreadsheet.Drives[target.TableName];
                    if (AppendDuplicateError(report, target, drive)) continue;

                    var local = ReadLocal(target);
                    var plan = SheetMergePlanner.PlanPush(target.Locales, local, drive.Rows, BaseOf(spreadsheet.BaseTables, target.TableName), forceKeys);
                    CollectWrites(writes, target, drive, local, plan);
                    spreadsheet.BaseTables[target.TableName] = plan.NextBase;

                    report.AppendLine($"[{target.TableName}] 書き換え {plan.Updates.Count} セル / 追加 {plan.Appends.Count} 行 / 削除 {plan.Deletes.Count} 行 / 書かなかった {plan.Skipped.Count} セル");
                    foreach (var cell in plan.Updates) AppendCell(report, "書き換え", cell);
                    foreach (var key in plan.Appends) report.AppendLine($"  追加 {key}");
                    foreach (var key in plan.Deletes) report.AppendLine($"  削除 {key}");
                    foreach (var cell in plan.Skipped) AppendCell(report, $"書かなかった ({SheetMergePlanner.Describe(cell.State)})", cell);
                }
                CollectBaseWrites(writes, spreadsheet);
                Execute(spreadsheet, writes);
            }
            return report.ToString();
        }

        /// <summary>
        /// シートの内容で StringTable を丸ごと置き換える (Unity Localization 標準の Pull)
        /// </summary>
        public static string PullAll()
        {
            var report = new StringBuilder();
            foreach (var collection in LinkedCollections(""))
            {
                var extension = collection.Extensions.OfType<GoogleSheetsExtension>().First();
                try
                {
                    var sheets = new GoogleSheets(extension.SheetsServiceProvider) { SpreadSheetId = extension.SpreadsheetId };
                    sheets.PullIntoStringTableCollection(extension.SheetId, collection, extension.Columns, createUndo: true);
                    report.AppendLine($"[{collection.TableCollectionName}] Pull しました");
                }
                catch (Exception e)
                {
                    report.AppendLine($"[{collection.TableCollectionName}] Pull に失敗: {e.Message}");
                }
            }
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static TranslationRows BaseOf(Dictionary<string, TranslationRows> tables, string tableName)
        {
            return tables.TryGetValue(tableName, out var rows) ? rows : new TranslationRows();
        }

        private static ValueRange CellRange(string title, int rowIndex, int columnIndex, string value)
        {
            return new()
            {
                Range = $"{Quote(title)}!{SheetColumn.IndexToColumnName(columnIndex)}{rowIndex + 1}",
                Values = new List<IList<object>>
        {
            new List<object>
            {
                value
            }
        },
            };
        }

        private static string Display(string value)
        {
            return value == null ? "(行なし)" : $"\"{value.Replace("\n", "\\n")}\"";
        }

        private static string CellText(IList<object> row, int index)
        {
            return index < row.Count ? row[index]?.ToString() ?? "" : "";
        }

        private static string Quote(string title)
        {
            return $"'{title.Replace("'", "''")}'";
        }

        private static IEnumerable<StringTableCollection> LinkedCollections(string tableFilter)
        {
            return LocalizationEditorSettings.GetStringTableCollections()
                .Where(c => c.Extensions.OfType<GoogleSheetsExtension>().Any(e => e.SheetsServiceProvider != null && !string.IsNullOrEmpty(e.SpreadsheetId)))
                .Where(c => string.IsNullOrEmpty(tableFilter) || c.TableCollectionName == tableFilter)
                .OrderBy(c => c.TableCollectionName);
        }

        private static IEnumerable<SpreadsheetData> Load(string tableFilter, string spreadsheetIdOverride)
        {
            var collections = LinkedCollections(tableFilter).ToList();
            if (collections.Count == 0) throw new ArgumentException($"スプレッドシートと連携した StringTable がありません: '{tableFilter}'");

            var groups = collections.GroupBy(c =>
            {
                var extension = c.Extensions.OfType<GoogleSheetsExtension>().First();
                return (Provider: extension.SheetsServiceProvider, SpreadsheetId: string.IsNullOrEmpty(spreadsheetIdOverride) ? extension.SpreadsheetId : spreadsheetIdOverride);
            });

            foreach (var group in groups)
            {
                EnsureAuthorized(group.Key.Provider);
                var service = group.Key.Provider.Service;
                var spreadsheetId = group.Key.SpreadsheetId;
                var properties = ReadSheetProperties(group.Key.Provider, spreadsheetId);
                var targets = group.Select(c => CreateTarget(c, properties, !string.IsNullOrEmpty(spreadsheetIdOverride))).ToList();
                var baseSheet = properties.FirstOrDefault(p => p.Title == BASE_SHEET_TITLE);

                // 通信の回数で待ち時間が決まるので、全テーブルと前回の同期時点の値を 1 回で読む
                var ranges = targets.Select(t => Quote(t.Title)).ToList();
                if (baseSheet != null) ranges.Add(Quote(BASE_SHEET_TITLE));
                var request = service.Spreadsheets.Values.BatchGet(spreadsheetId);
                request.Ranges = ranges;
                var valueRanges = request.Execute().ValueRanges;

                var baseValues = baseSheet != null ? valueRanges[targets.Count].Values ?? new List<IList<object>>() : new List<IList<object>>();
                yield return new SpreadsheetData
                {
                    Service = service,
                    SpreadsheetId = spreadsheetId,
                    Targets = targets,
                    Drives = targets.Select((t, i) => (t.TableName, Drive: ParseDrive(t, valueRanges[i].Values ?? new List<IList<object>>())))
                        .ToDictionary(x => x.TableName, x => x.Drive),
                    BaseSheet = baseSheet,
                    BaseRowCount = baseValues.Count,
                    BaseTables = ParseBase(baseValues),
                };
            }
        }

        // トークンが無いと Service がブラウザ認証を始め、同期待ちの RunSynchronously が例外になるため、通信の前に止める
        private static void EnsureAuthorized(SheetsServiceProvider provider)
        {
            if (provider.Authentication != AuthenticationType.OAuth) return;
            var tokenDirectory = $"Library/Google/{provider.name}";
            if (Directory.Exists(tokenDirectory) && Directory.EnumerateFiles(tokenDirectory).Any()) return;
            throw new InvalidOperationException(
                $"Google の認証トークンがありません。{AssetDatabase.GetAssetPath(provider)} のインスペクタの Authorize... からブラウザで認証してください");
        }

        private static SheetTarget CreateTarget(StringTableCollection collection, IList<SheetProperties> properties, bool isOverridden)
        {
            var extension = collection.Extensions.OfType<GoogleSheetsExtension>().First();
            // sheetId はスプレッドシートごとに振られるので、別のスプレッドシートを指したときはシート名 (= テーブル名) で探す
            var sheet = properties.FirstOrDefault(p => p.SheetId == extension.SheetId)
                        ?? (isOverridden ? properties.FirstOrDefault(p => p.Title == collection.TableCollectionName) : null)
                        ?? throw new InvalidOperationException($"'{collection.TableCollectionName}' のシート (sheetId {extension.SheetId}) がスプレッドシートにありません");

            return new SheetTarget
            {
                Collection = collection,
                Extension = extension,
                TableName = collection.TableCollectionName,
                Title = sheet.Title,
                SheetId = sheet.SheetId ?? 0,
                GridRowCount = sheet.GridProperties?.RowCount ?? 0,
                KeyColumnIndex = extension.Columns.OfType<KeyColumn>().Single().ColumnIndex,
                LocaleColumns = extension.Columns.OfType<LocaleColumn>().Select(c => (c.LocaleIdentifier.Code, c.ColumnIndex)).ToList(),
            };
        }

        // トークンの更新は最初の通信で行われるので、認証切れはここで分かる
        private static IList<SheetProperties> ReadSheetProperties(SheetsServiceProvider provider, string spreadsheetId)
        {
            var request = provider.Service.Spreadsheets.Get(spreadsheetId);
            request.Fields = "sheets.properties";
            try
            {
                return request.Execute().Sheets.Select(s => s.Properties).ToList();
            }
            catch (TokenResponseException e)
            {
                throw new InvalidOperationException(
                    $"Google の認証が切れています ({e.Error?.Error})。{AssetDatabase.GetAssetPath(provider)} のインスペクタの Authorize... からブラウザで認証し直してください", e);
            }
        }

        private static TranslationRows ReadLocal(SheetTarget target)
        {
            var rows = new TranslationRows();
            var tables = target.LocaleColumns.ToDictionary(c => c.Locale, c => target.Collection.GetTable(c.Locale) as StringTable);
            foreach (var entry in target.Collection.SharedData.Entries)
            {
                rows.AddKey(entry.Key);
                foreach (var (locale, table) in tables)
                {
                    var value = table?.GetEntry(entry.Id)?.Value;
                    if (value != null) rows.Set(entry.Key, locale, value);
                }
            }
            return rows;
        }

        private static DriveSheet ParseDrive(SheetTarget target, IList<IList<object>> values)
        {
            var drive = new DriveSheet
            {
                Rows = new TranslationRows(),
                RowIndexByKey = new Dictionary<string, int>(),
                RowCount = values.Count,
                DuplicateKeys = new List<string>(),
            };

            // 1 行目は見出し
            for (var i = 1; i < values.Count; i++)
            {
                var key = CellText(values[i], target.KeyColumnIndex);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (drive.RowIndexByKey.ContainsKey(key))
                {
                    drive.DuplicateKeys.Add(key);
                    continue;
                }

                drive.RowIndexByKey.Add(key, i);
                drive.Rows.AddKey(key);
                foreach (var (locale, columnIndex) in target.LocaleColumns)
                    drive.Rows.Set(key, locale, CellText(values[i], columnIndex));
            }
            return drive;
        }

        private static Dictionary<string, TranslationRows> ParseBase(IList<IList<object>> values)
        {
            var tables = new Dictionary<string, TranslationRows>();
            foreach (var row in values.Skip(1))
            {
                var table = CellText(row, 0);
                var key = CellText(row, 1);
                if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(key)) continue;
                if (!tables.TryGetValue(table, out var rows)) tables.Add(table, rows = new TranslationRows());
                rows.Set(key, CellText(row, 2), CellText(row, 3));
            }
            return tables;
        }

        private static void CollectWrites(SheetWrites writes, SheetTarget target, DriveSheet drive, TranslationRows local, SheetPushPlan plan)
        {
            var columnByLocale = target.LocaleColumns.ToDictionary(c => c.Locale, c => c.ColumnIndex);
            foreach (var cell in plan.Updates)
                writes.Values.Add(CellRange(target.Title, drive.RowIndexByKey[cell.Key], columnByLocale[cell.Locale], cell.Local));

            for (var i = 0; i < plan.Appends.Count; i++)
            {
                var key = plan.Appends[i];
                var rowIndex = drive.RowCount + i;
                writes.Values.Add(CellRange(target.Title, rowIndex, target.KeyColumnIndex, key));
                foreach (var (locale, columnIndex) in target.LocaleColumns)
                    writes.Values.Add(CellRange(target.Title, rowIndex, columnIndex, local.Get(key, locale)));
            }
            AddRowsIfShort(writes, target.SheetId, target.GridRowCount, drive.RowCount + plan.Appends.Count);

            // 下の行から消し、消す前の行番号のまま指せるようにする
            writes.Deletes.AddRange(plan.Deletes.Select(k => drive.RowIndexByKey[k]).OrderByDescending(i => i)
                .Select(i => new Request
                {
                    DeleteDimension = new DeleteDimensionRequest
                    {
                        Range = new DimensionRange { SheetId = target.SheetId, Dimension = "ROWS", StartIndex = i, EndIndex = i + 1 },
                    },
                }));
        }

        private static void CollectBaseWrites(SheetWrites writes, SpreadsheetData spreadsheet)
        {
            var values = new List<IList<object>> { new List<object> { "Table", "Key", "Locale", "Value" } };
            foreach (var (table, rows) in spreadsheet.BaseTables.OrderBy(t => t.Key))
                foreach (var key in rows.Keys)
                    foreach (var (locale, value) in rows.GetRow(key))
                        values.Add(new List<object> { table, key, locale, value });

            // 前回より行が減ったときに古い行が残らないよう、空の行で上書きする
            while (values.Count < spreadsheet.BaseRowCount) values.Add(new List<object> { "", "", "", "" });

            if (spreadsheet.BaseSheet == null)
            {
                writes.Structure.Insert(0, new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties { Title = BASE_SHEET_TITLE, Hidden = true, GridProperties = new GridProperties { RowCount = values.Count, ColumnCount = 4 } },
                    },
                });
            }
            else
            {
                AddRowsIfShort(writes, spreadsheet.BaseSheet.SheetId ?? 0, spreadsheet.BaseSheet.GridProperties?.RowCount ?? 0, values.Count);
            }
            writes.Base = new ValueRange { Range = $"{Quote(BASE_SHEET_TITLE)}!A1", Values = values };
        }

        // シートの行数を超える範囲へは値を書けないので、先に行を足しておく
        private static void AddRowsIfShort(SheetWrites writes, int sheetId, int gridRowCount, int required)
        {
            if (required <= gridRowCount) return;
            writes.Structure.Add(new Request { AppendDimension = new AppendDimensionRequest { SheetId = sheetId, Dimension = "ROWS", Length = required - gridRowCount } });
        }

        private static void Execute(SpreadsheetData spreadsheet, SheetWrites writes)
        {
            var sheets = spreadsheet.Service.Spreadsheets;
            if (writes.Structure.Count > 0)
                sheets.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = writes.Structure }, spreadsheet.SpreadsheetId).Execute();
            if (writes.Values.Count > 0)
                sheets.Values.BatchUpdate(new BatchUpdateValuesRequest { ValueInputOption = "RAW", Data = writes.Values }, spreadsheet.SpreadsheetId).Execute();
            if (writes.Deletes.Count > 0)
                sheets.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = writes.Deletes }, spreadsheet.SpreadsheetId).Execute();

            // 途中で失敗したときに、書けていない変更を同期済みとして残さないよう、前回の同期時点の値は最後に書く
            var update = sheets.Values.Update(writes.Base, spreadsheet.SpreadsheetId, writes.Base.Range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            update.Execute();
        }

        private static bool AppendDuplicateError(StringBuilder report, SheetTarget target, DriveSheet drive)
        {
            if (drive.DuplicateKeys.Count == 0) return false;
            report.AppendLine($"[{target.TableName}] Drive にキーの重複があるため扱いません: {string.Join(", ", drive.DuplicateKeys.Distinct())}");
            return true;
        }

        private static void AppendCell(StringBuilder report, string label, SheetCell cell)
        {
            report.AppendLine($"  {label} {cell.Key} [{cell.Locale}]");
            report.AppendLine($"    ローカル: {Display(cell.Local)}");
            report.AppendLine($"    Drive   : {Display(cell.Drive)}");
            report.AppendLine($"    前回同期: {Display(cell.Base)}");
        }
    }
}
