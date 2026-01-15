using System;
using Microsoft.Extensions.Logging;
using ZLogger;
using Random = System.Random;

/// <summary>
/// ランダム数値生成を担当するシングルトンマネージャー
/// シード値を使用した再現可能なランダム生成を提供
/// </summary>
public class RandomManager
{
    // public プロパティ
    public static RandomManager Instance { get; } = new RandomManager();

    private string _seedText;
    private Random _random;
    private ILogger _logger;
    
    public string GetSeedText() => _seedText;

    private RandomManager() { }

    // public メソッド
    /// <summary>
    /// シード値を初期化する
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <param name="seedText">シード文字列（空の場合は自動生成）</param>
    public void Initialize(ILogger logger, string seedText = "")
    {
        _logger = logger;

        // シードテキストが空の場合は新規生成
        if (string.IsNullOrEmpty(seedText))
        {
            var guid = Guid.NewGuid();
            _seedText = guid.ToString("N")[..8];
            _logger.ZLogInformation($"[RandomManager] 新規シード生成: {_seedText}");
        }
        else
        {
            _seedText = seedText;
            _logger.ZLogInformation($"[RandomManager] 指定シード使用: {_seedText}");
        }

        var seed = _seedText.GetHashCode();
        _random = new Random(seed);
    }

    /// <summary>
    /// 指定された確率でtrueを返す
    /// </summary>
    /// <param name="probability">確率（0.0～1.0）</param>
    /// <returns>確率判定結果</returns>
    public bool Chance(float probability)
    {
        var r = _random.NextDouble();
        _logger.ZLogTrace($"[RandomManager] Chance: r={r}, probability={probability}, result={(r < probability)}");
        return r < probability;
    }

    /// <summary>
    /// 指定された範囲内のランダムなfloat値を生成する
    /// </summary>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    /// <returns>指定範囲内のランダムfloat値</returns>
    public float RandomFloat(float min, float max)
    {
        var r = (float)(_random.NextDouble() * (max - min) + min);
        _logger.ZLogTrace($"RandomManager: RandomFloat: r={r} in [{min}, {max})");
        return r;
    }

    /// <summary>
    /// 指定された範囲内のランダムなint値を生成する
    /// </summary>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値（exclusive）</param>
    /// <returns>指定範囲内のランダムint値</returns>
    public int RandomInt(int min, int max)
    {
        var r = _random.Next(min, max);
        _logger.ZLogTrace($"[RandomManager] RandomInt: r={r} in [{min}, {max})");
        return r;
    }
}