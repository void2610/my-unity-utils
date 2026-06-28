using System;

public interface IConfigurableCinematicEffect
{
    public void ApplyConfig(CinematicEffectConfig config);
    public void ResetConfig();
}

public abstract class ConfigurableCinematicEffectBase<TConfig> : CinematicEffectBase, IConfigurableCinematicEffect
    where TConfig : CinematicEffectConfig, new()
{
    private readonly TConfig _defaultConfig;

    protected TConfig CurrentConfig { get; private set; }

    /// <param name="defaultConfig">デフォルト設定。null の場合は新規インスタンスを使用。</param>
    protected ConfigurableCinematicEffectBase(TConfig defaultConfig = null)
    {
        _defaultConfig = defaultConfig ?? new TConfig();
        ResetConfig();
    }

    public void ResetConfig() => CurrentConfig = (TConfig)_defaultConfig.Clone();

    public void ApplyConfig(CinematicEffectConfig config)
    {
        if (config is not TConfig typedConfig)
        {
            throw new InvalidOperationException($"設定型 {config.GetType().Name} は {GetType().Name} に適用できません。");
        }

        CurrentConfig = (TConfig)typedConfig.Clone();
    }
}
