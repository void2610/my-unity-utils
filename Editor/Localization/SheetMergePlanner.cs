using System;
using System.Collections.Generic;
using System.Linq;

namespace Void2610.UnityTemplate.Editor.Localization
{
    public enum SheetCellState
    {
        /// <summary> ローカルと Drive が同じ値 </summary>
        InSync,
        /// <summary> Drive は前回の同期時点のままで、ローカルだけ変わった </summary>
        LocalChanged,
        /// <summary> ローカルは前回の同期時点のままで、Drive だけ変わった </summary>
        DriveChanged,
        /// <summary> ローカルと Drive の両方が前回の同期時点から変わり、値も違う </summary>
        Conflict,
    }

    /// <summary>
    /// 1 つのキーの 1 つのロケールについて、ローカル・Drive・前回の同期時点の値を並べたもの。null はその行が無いことを表す
    /// </summary>
    public sealed class SheetCell
    {
        public string Key { get; }
        public string Locale { get; }
        public string Local { get; }
        public string Drive { get; }
        public string Base { get; }
        public SheetCellState State { get; }

        public SheetCell(string key, string locale, string local, string drive, string baseValue, SheetCellState state)
        {
            Key = key;
            Locale = locale;
            Local = local;
            Drive = drive;
            Base = baseValue;
            State = state;
        }
    }

    public sealed class SheetPushPlan
    {
        public List<SheetCell> Updates { get; } = new();
        public List<string> Appends { get; } = new();
        public List<string> Deletes { get; } = new();
        public List<SheetCell> Skipped { get; } = new();
        public TranslationRows NextBase { get; } = new();
    }

    /// <summary>
    /// 前回の同期時点の値を基準に、ローカルと Drive のどちらで変わったかを決め、Drive へ書き戻す内容を組み立てる
    /// </summary>
    public static class SheetMergePlanner
    {
        public static string Describe(SheetCellState state) => state switch
        {
            SheetCellState.InSync => "一致",
            SheetCellState.LocalChanged => "ローカルの変更",
            SheetCellState.DriveChanged => "Drive の変更",
            SheetCellState.Conflict => "衝突",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

        /// <summary>
        /// Pull で Drive の値に置き換わって失われるローカルの変更か。Pull は Drive に無いキーを消さずに残すので、Drive に行があるセルだけが対象
        /// </summary>
        public static bool IsLostByPull(SheetCell cell) => cell.Drive != null && cell.State is SheetCellState.LocalChanged or SheetCellState.Conflict;
        public static SheetCellState Classify(string local, string drive, string baseValue)
        {
            if (local == drive) return SheetCellState.InSync;
            if (drive == baseValue) return SheetCellState.LocalChanged;
            if (local == baseValue) return SheetCellState.DriveChanged;
            return SheetCellState.Conflict;
        }

        public static List<SheetCell> Compare(IReadOnlyList<string> locales, TranslationRows local, TranslationRows drive, TranslationRows baseRows)
        {
            var keys = local.Keys.Concat(drive.Keys).Concat(baseRows.Keys).Distinct();
            return keys
                .SelectMany(key => locales.Select(locale =>
                {
                    var l = local.Get(key, locale);
                    var d = drive.Get(key, locale);
                    var b = baseRows.Find(key, locale);
                    return new SheetCell(key, locale, l, d, b, Classify(l, d, b));
                }))
                .ToList();
        }

        /// <summary>
        /// ローカルで変わったセルだけを書く。行の追加と削除は、その行の変わったセルがすべて書けるときだけ行う
        /// </summary>
        public static SheetPushPlan PlanPush(IReadOnlyList<string> locales, TranslationRows local, TranslationRows drive, TranslationRows baseRows, ICollection<string> forceKeys)
        {
            var plan = new SheetPushPlan();
            var applied = new HashSet<SheetCell>();

            foreach (var group in Compare(locales, local, drive, baseRows).GroupBy(c => c.Key))
            {
                var key = group.Key;
                var cells = group.ToList();
                var changed = cells.Where(c => c.State != SheetCellState.InSync).ToList();
                bool CanWrite(SheetCell c) => c.State == SheetCellState.LocalChanged
                                              || (c.State == SheetCellState.Conflict && forceKeys.Contains(key));

                if (local.HasKey(key) && drive.HasKey(key))
                {
                    foreach (var cell in changed)
                    {
                        if (CanWrite(cell)) applied.Add(cell);
                        else plan.Skipped.Add(cell);
                    }
                    plan.Updates.AddRange(changed.Where(applied.Contains));
                }
                else if (changed.Count > 0 && changed.All(CanWrite))
                {
                    changed.ForEach(c => applied.Add(c));
                    if (drive.HasKey(key)) plan.Deletes.Add(key);
                    else plan.Appends.Add(key);
                }
                else
                {
                    plan.Skipped.AddRange(changed);
                }

                foreach (var cell in cells)
                {
                    var next = cell.State == SheetCellState.InSync || applied.Contains(cell) ? cell.Local : cell.Base;
                    if (next != null) plan.NextBase.Set(key, cell.Locale, next);
                }
            }

            return plan;
        }
    }
}
