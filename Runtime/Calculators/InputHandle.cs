using System;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// A stable reference to one of a calculator's declared inputs, handed out by
    /// <see cref="CalculatorBuilder.AddInput{TName,TEntity}"/> during <see cref="IPropertyCalculator.Declare"/>.
    /// A calculator stores the handle and later resolves it against the live inputs via
    /// <see cref="CalculationContext.Inputs"/> / <see cref="CalculationContext.Value"/>, so input order
    /// no longer has to match the code that reads it.
    /// </summary>
    public readonly struct InputHandle
    {
        // Stored as (index + 1) so that default(InputHandle) — an unassigned field — is detectable
        // as the zero value and rejected before it is used as an index.
        private readonly int _indexPlusOne;

        internal InputHandle(int index)
        {
            _indexPlusOne = index + 1;
        }

        internal int Index
        {
            get
            {
                if (_indexPlusOne == 0)
                {
                    throw new InvalidOperationException(
                        "This input handle was not initialized via CalculatorBuilder.AddInput.");
                }

                return _indexPlusOne - 1;
            }
        }
    }
}
