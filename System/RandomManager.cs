using System;
using UnityEngine;
using Random = System.Random;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ランダム数値生成を担当するシングルトンマネージャー
    /// シード値を使用した再現可能なランダム生成を提供
    ///
    /// 使用例:
    /// - ローグライクゲームのマップ生成
    /// - テスト環境での再現可能な乱数
    /// - デバッグ時の挙動確認
    ///
    /// 使い方:
    /// 1. RandomManager.Instance.Initialize("seed123"); でシードを設定
    /// 2. RandomManager.Instance.RandomInt(0, 10); で乱数生成
    /// 3. 同じシードを使えば同じ乱数列が生成される
    /// </summary>
    public class RandomManager
    {
        public static RandomManager Instance { get; } = new RandomManager();

        private string _seedText;
        private Random _random;
        private bool _enableLogging = false;

        /// <summary>
        /// 現在のシード文字列を取得
        /// </summary>
        public string GetSeedText() => _seedText;

        /// <summary>
        /// ログ出力の有効/無効を設定
        /// </summary>
        public bool EnableLogging
        {
            get => _enableLogging;
            set => _enableLogging = value;
        }

        private RandomManager() { }

        /// <summary>
        /// シード値を初期化する
        /// </summary>
        /// <param name="seedText">シード文字列（空の場合は自動生成）</param>
        public void Initialize(string seedText = "")
        {
            // シードテキストが空の場合は新規生成
            if (string.IsNullOrEmpty(seedText))
            {
                var guid = Guid.NewGuid();
                _seedText = guid.ToString("N")[..8];
                if (_enableLogging)
                    Debug.Log($"RandomManager: 新規シード生成: {_seedText}");
            }
            else
            {
                _seedText = seedText;
                if (_enableLogging)
                    Debug.Log($"RandomManager: 指定シード使用: {_seedText}");
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
            if (_random == null)
            {
                Debug.LogWarning("RandomManager: Initialize()を先に呼び出してください");
                Initialize();
            }

            var r = _random.NextDouble();
            if (_enableLogging)
                Debug.Log($"RandomManager: Chance: r={r}, probability={probability}, result={(r < probability)}");
            return r < probability;
        }

        /// <summary>
        /// 指定された範囲内のランダムなfloat値を生成する
        /// </summary>
        /// <param name="min">最小値（inclusive）</param>
        /// <param name="max">最大値（exclusive）</param>
        /// <returns>指定範囲内のランダムfloat値</returns>
        public float RandomFloat(float min, float max)
        {
            if (_random == null)
            {
                Debug.LogWarning("RandomManager: Initialize()を先に呼び出してください");
                Initialize();
            }

            var r = (float)(_random.NextDouble() * (max - min) + min);
            if (_enableLogging)
                Debug.Log($"RandomManager: RandomFloat: r={r} in [{min}, {max})");
            return r;
        }

        /// <summary>
        /// 指定された範囲内のランダムなint値を生成する
        /// </summary>
        /// <param name="min">最小値（inclusive）</param>
        /// <param name="max">最大値（exclusive）</param>
        /// <returns>指定範囲内のランダムint値</returns>
        public int RandomInt(int min, int max)
        {
            if (_random == null)
            {
                Debug.LogWarning("RandomManager: Initialize()を先に呼び出してください");
                Initialize();
            }

            var r = _random.Next(min, max);
            if (_enableLogging)
                Debug.Log($"RandomManager: RandomInt: r={r} in [{min}, {max})");
            return r;
        }

        /// <summary>
        /// 現在のRandomインスタンスを取得
        /// より高度な乱数操作が必要な場合に使用
        /// </summary>
        public Random GetRandom()
        {
            if (_random == null)
            {
                Debug.LogWarning("RandomManager: Initialize()を先に呼び出してください");
                Initialize();
            }
            return _random;
        }
    }
}
