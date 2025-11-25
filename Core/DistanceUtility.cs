using UnityEngine;

/// <summary>
/// 2点間の距離計算を提供するユーティリティクラス
/// </summary>
public static class DistanceUtility
{
    /// <summary>
    /// マンハッタン距離を計算（菱形距離）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>マンハッタン距離</returns>
    public static int GetManhattanDistance(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y);
    }
    
    /// <summary>
    /// チェビシェフ距離を計算（正方形距離）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>チェビシェフ距離</returns>
    public static int GetChebyshevDistance(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Max(Mathf.Abs(pos1.x - pos2.x), Mathf.Abs(pos1.y - pos2.y));
    }
    
    /// <summary>
    /// ユークリッド距離を計算（円形距離）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>ユークリッド距離</returns>
    public static float GetEuclideanDistance(Vector2Int pos1, Vector2Int pos2)
    {
        var dx = pos1.x - pos2.x;
        var dy = pos1.y - pos2.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// ユークリッド距離の二乗を計算（平方根計算を避けるため高速）
    /// </summary>
    /// <param name="pos1">位置1</param>
    /// <param name="pos2">位置2</param>
    /// <returns>ユークリッド距離の二乗</returns>
    public static int GetSquaredEuclideanDistance(Vector2Int pos1, Vector2Int pos2)
    {
        var dx = pos1.x - pos2.x;
        var dy = pos1.y - pos2.y;
        return dx * dx + dy * dy;
    }
}