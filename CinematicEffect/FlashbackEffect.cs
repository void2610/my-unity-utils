using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 回想（フラッシュバック）を表す複合演出。
/// 映画的なフラッシュバックの定番表現を組み合わせる:
/// <list type="bullet">
///   <item>開始時の白フラッシュ（意識が飛ぶ瞬間）</item>
///   <item>脱色・暖色寄りのカラーグレード（トーンマッピング相当）</item>
///   <item>フィルムグレイン（古い記憶・フィルム質感）</item>
///   <item>ビネット（周辺減光で中心へ意識を集める）</item>
/// </list>
/// 各要素は既存の単体 CinematicEffect を内部でオーケストレーションするため、他プロジェクトでもこのクラス単体で再利用できる。
/// </summary>
public sealed class FlashbackEffect : ConfigurableCinematicEffectBase<FlashbackConfig>
{
    public override string EffectName => "フラッシュバック";

    private const float FlashEnterDuration = 0.06f;
    private const float FlashExitDuration = 0.35f;

    private readonly ColorGradeEffect _grade;
    private readonly FilmGrainEffect _grain;
    private readonly VignetteEffect _vignette;
    private readonly ImageFlashEffect _flash;

    public FlashbackEffect(GameObject postProcessOwner = null)
    {
        _grade = new ColorGradeEffect(postProcessOwner);
        _grain = new FilmGrainEffect(postProcessOwner);
        _vignette = new VignetteEffect(postProcessOwner);
        _flash = new ImageFlashEffect();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        var config = CurrentConfig;

        _grade.ApplyConfig(new ColorGradeConfig(1f, config.Saturation, config.ColorFilter, config.Contrast, config.PostExposure, config.EnterDuration, config.ExitDuration, config.Ease));
        _grain.ApplyConfig(new FilmGrainConfig(1f, config.GrainIntensity, UnityEngine.Rendering.Universal.FilmGrainLookup.Thin1, config.EnterDuration, config.ExitDuration, config.Ease));
        _vignette.ApplyConfig(new VignetteConfig(1f, config.VignetteIntensity, 0.7f, config.EnterDuration, config.ExitDuration, config.Ease));

        // グレード・グレイン・ビネットはサスティン型なので fire-and-forget で重ね、停止時にまとめて戻す
        _grade.PlayAsync(ct).Forget();
        _grain.PlayAsync(ct).Forget();
        _vignette.PlayAsync(ct).Forget();

        if (config.FlashOnEnter)
        {
            _flash.ApplyConfig(new ImageFlashConfig(Color.white, FlashEnterDuration, FlashExitDuration, 0f, config.Ease));
            _flash.PlayAsync(ct).Forget();
        }

        if (config.HoldDuration > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(config.HoldDuration), cancellationToken: ct);
            await StopSustainAsync(ct);
        }
        else
        {
            // Stop / ResetImmediate が呼ばれるまで持続する
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }

    protected override UniTask OnStopAsync(CancellationToken ct) => StopSustainAsync(ct);

    protected override void OnResetImmediate()
    {
        _grade?.ResetImmediate();
        _grain?.ResetImmediate();
        _vignette?.ResetImmediate();
        _flash?.ResetImmediate();
    }

    // サスティンしている3要素を同時にフェードアウトして戻す
    private async UniTask StopSustainAsync(CancellationToken ct)
    {
        await UniTask.WhenAll(
            _grade.StopAsync(ct),
            _grain.StopAsync(ct),
            _vignette.StopAsync(ct));
    }

    public override void Dispose()
    {
        base.Dispose();
        _grade.Dispose();
        _grain.Dispose();
        _vignette.Dispose();
        _flash.Dispose();
    }
}
