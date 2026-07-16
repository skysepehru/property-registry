using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Collects a calculator's declared inputs and output. Passed to
    /// <see cref="IPropertyCalculator.Declare"/> exactly once, at registration time (the cold path), so
    /// it may allocate freely. Each <see cref="AddInput{TName,TEntity}"/> hands back an
    /// <see cref="InputHandle"/> the calculator keeps and later resolves against the live inputs.
    /// </summary>
    public sealed class CalculatorBuilder
    {
        private readonly List<PropertyFilter> _inputs = new();
        private PropertyFilter _output;
        private bool _hasOutput;

        /// <summary>
        /// Declares an input the calculator reads, returning a handle to it. The order of
        /// <see cref="AddInput{TName,TEntity}"/> calls does not affect calculation — reads go through the
        /// returned handle, not a positional index.
        /// </summary>
        public InputHandle AddInput<TName, TEntity>(TName name, TEntity entity)
            where TName : unmanaged, Enum
            where TEntity : unmanaged, Enum
        {
            return AddInput(PropertyFilter.New(name, entity));
        }

        /// <summary>
        /// Declares the single property this calculator produces. Throws if called more than once.
        /// </summary>
        public void SetOutput<TName, TEntity>(TName name, TEntity entity)
            where TName : unmanaged, Enum
            where TEntity : unmanaged, Enum
        {
            SetOutput(PropertyFilter.New(name, entity));
        }

        internal InputHandle AddInput(PropertyFilter filter)
        {
            var index = _inputs.Count;
            _inputs.Add(filter);
            return new InputHandle(index);
        }

        internal void SetOutput(PropertyFilter filter)
        {
            if (_hasOutput)
            {
                throw new InvalidOperationException("SetOutput was already called; a calculator has exactly one output.");
            }

            _output = filter;
            _hasOutput = true;
        }

        internal PropertyFilter[] BuildInputFilters() => _inputs.ToArray();

        internal PropertyFilter OutputFilter
        {
            get
            {
                if (!_hasOutput)
                {
                    throw new InvalidOperationException(
                        "Calculator did not declare an output; call CalculatorBuilder.SetOutput in Declare.");
                }

                return _output;
            }
        }
    }
}
