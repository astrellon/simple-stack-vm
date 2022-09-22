using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable

namespace SimpleStackVM
{
    public interface IReadOnlyFixedStack<T>
    {
        int Index { get; }
        IReadOnlyList<T> Data { get; }
        bool TryPeek([MaybeNullWhen(false)] [NotNullWhen(true)] out T? result);
    }

    public class FixedStack<T> : IReadOnlyFixedStack<T>
    {
        #region Fields
        private readonly T[] data;
        private int index = -1;

        public int Index => this.index;
        public IReadOnlyList<T> Data => this.data;
        #endregion

        #region Constructor
        public FixedStack(int size)
        {
            this.data = new T[size];
        }
        #endregion

        #region Methods
        public void Clear()
        {
            this.index = -1;
        }

        public void Swap(int topOffset)
        {
            var newIndex = this.index - topOffset;
            if (newIndex < 0 || newIndex >= this.index)
            {
                throw new ArgumentException($"Unable to stack swap, invalid offset");
            }

            var top = this.data[this.index];
            var other = this.data[newIndex];

            this.data[this.index] = other;
            this.data[newIndex] = top;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            if (this.index >= this.data.Length)
            {
                return false;
            }
            this.data[++this.index] = item;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop([MaybeNullWhen(false)] [NotNullWhen(true)] out T? result)
        {
            if (this.index < 0)
            {
                result = default(T);
                return false;
            }

            result = this.data[this.index--];
            return result != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek([MaybeNullWhen(false)] [NotNullWhen(true)] out T? result)
        {
            if (this.index < 0)
            {
                result = default(T);
                return false;
            }

            result = this.data[this.index];
            return result != null;
        }
        #endregion
    }
}