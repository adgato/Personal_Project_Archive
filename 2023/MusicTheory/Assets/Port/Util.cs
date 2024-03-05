using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Music_Theory
{
    public static class Mathx
    {
        public static System.Random Random
        {
            get
            {
                if (_random == null)
                    _random = new System.Random();
                return _random;
            }
        }
        private static System.Random _random;

        

        /// <summary>
        /// Mod that always returns positive...
        /// </summary>
        public static int Mod(int x, int m) => (x % m + m) % m;
        /// <summary>
        /// Mod that always returns positive...
        /// </summary>
        public static double Mod(double x, double m) => (x % m + m) % m;
        public static double Round(double x, double nearest) => Math.Round(x / nearest) * nearest;
    }

    public static class CoroutineEx
    {
        public static Coroutine StartCoroutineSequence(this MonoBehaviour monoBehaviour, params IEnumerator[] actions) => monoBehaviour.StartCoroutine(Sequence(actions));

        public static IEnumerator Sequence(params IEnumerator[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
                while (actions[i].MoveNext())
                    if (actions[i].Current != null)
                        yield return actions[i].Current;
            yield return null;
        }

        public static IEnumerator WaitForSeconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        public static IEnumerator WaitUntil(Func<bool> predicate)
        {
            yield return new WaitUntil(predicate);
        }
        public static IEnumerator WaitWhile(Func<bool> predicate)
        {
            yield return new WaitWhile(predicate);
        }

        public static IEnumerator Chain(Action action)
        {
            action();
            yield return null;
        }
        public static IEnumerator Chain<T>(Func<T> action)
        {
            action();
            yield return null;
        }
    }
}
