using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// uGUI の Image のスプライトを一定間隔で順に差し替えるコマ送りアニメーション
    /// 有効化のたびに先頭コマから再生し直すので、SetActive で使い回すオブジェクトにそのまま付けられる
    /// </summary>
    public class ImageFrameAnimator : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 5f;

        private int _frame;
        private float _timer;

        private void OnEnable()
        {
            _frame = 0;
            _timer = 0f;
            image.sprite = frames[0];
        }

        private void Update()
        {
            var interval = 1f / framesPerSecond;
            _timer += Time.deltaTime;
            if (_timer < interval) return;

            _timer -= interval;
            _frame = (_frame + 1) % frames.Length;
            image.sprite = frames[_frame];
        }
    }
}
