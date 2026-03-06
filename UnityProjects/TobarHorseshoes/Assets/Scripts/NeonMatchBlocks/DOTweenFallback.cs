#if !DOTWEEN && !DOTWEEN_ENABLED
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Temporary fallback for projects without DOTween installed yet.
// It applies final values instantly so gameplay remains functional.
// Remove this file after installing DOTween if your project defines no DOTween symbols.
namespace DG.Tweening
{
    public enum Ease
    {
        OutQuad,
        InOutSine,
        OutBounce,
        OutSine,
        OutCubic
    }

    public enum RotateMode
    {
        FastBeyond360
    }

    public enum LoopType
    {
        Yoyo
    }

    public delegate void TweenCallback();

    public class Tween
    {
        protected bool active;
        protected Action onComplete;

        public Tween()
        {
            active = false;
        }

        public virtual Tween SetEase(Ease ease)
        {
            return this;
        }

        public virtual Tween SetLoops(int loops, LoopType loopType = LoopType.Yoyo)
        {
            return this;
        }

        public virtual Tween OnComplete(Action callback)
        {
            onComplete = callback;
            onComplete?.Invoke();
            return this;
        }

        public virtual void Kill()
        {
            active = false;
        }

        public virtual bool IsActive()
        {
            return active;
        }
    }

    public class Sequence : Tween
    {
        public Sequence()
        {
            active = false;
        }

        public Sequence Join(Tween tween)
        {
            return this;
        }

        public Sequence Append(Tween tween)
        {
            return this;
        }

        public Sequence InsertCallback(float atPosition, TweenCallback callback)
        {
            callback?.Invoke();
            return this;
        }
    }

    internal sealed class InstantTween : Tween
    {
        public InstantTween(Action apply)
        {
            active = false;
            apply?.Invoke();
        }
    }

    public static class DOTween
    {
        public static Sequence Sequence()
        {
            return new Sequence();
        }

        public static Tween To(Func<float> getter, Action<float> setter, float endValue, float duration)
        {
            return new InstantTween(() => setter?.Invoke(endValue));
        }
    }

    public static class ShortcutExtensions
    {
        public static Tween DOLocalMoveY(this Transform target, float endValue, float duration)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                Vector3 p = target.localPosition;
                p.y = endValue;
                target.localPosition = p;
            });
        }

        public static Tween DOLocalMove(this Transform target, Vector3 endValue, float duration)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                target.localPosition = endValue;
            });
        }

        public static Tween DOLocalRotate(this Transform target, Vector3 endValue, float duration, RotateMode mode = RotateMode.FastBeyond360)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                target.localEulerAngles = endValue;
            });
        }

        public static Tween DOPunchScale(this Transform target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            return new InstantTween(null);
        }

        public static Tween DOPunchPosition(this Transform target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            return new InstantTween(null);
        }

        public static Tween DOAnchorPosY(this RectTransform target, float endValue, float duration)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                Vector2 p = target.anchoredPosition;
                p.y = endValue;
                target.anchoredPosition = p;
            });
        }

        public static Tween DOFade(this TMP_Text target, float endValue, float duration)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                Color c = target.color;
                c.a = endValue;
                target.color = c;
            });
        }

        public static Tween DOFade(this Graphic target, float endValue, float duration)
        {
            return new InstantTween(() =>
            {
                if (target == null) return;
                Color c = target.color;
                c.a = endValue;
                target.color = c;
            });
        }

        public static void DOKill(this Component target)
        {
            // no-op fallback
        }
    }
}
#endif
