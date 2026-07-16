namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Computes one derived property from a set of inputs. <see cref="Declare"/> runs once, at
    /// registration time, to name the inputs and output (cold path — allocation is fine).
    /// <see cref="Calculate"/> runs every tick the calculator is dirty and must allocate nothing.
    /// </summary>
    public interface IPropertyCalculator
    {
        /// <summary>
        /// Declares this calculator's inputs and output. Called exactly once, at registration time.
        /// Store the <see cref="InputHandle"/> returned by each
        /// <see cref="CalculatorBuilder.AddInput{TName,TEntity}"/> to read that input in
        /// <see cref="Calculate"/>.
        /// </summary>
        void Declare(CalculatorBuilder builder);

        /// <summary>
        /// Computes the output from the current inputs. Runs on the hot path — resolve inputs via the
        /// stored handles (<see cref="CalculationContext.Inputs"/> / <see cref="CalculationContext.Value"/>)
        /// and write results through <see cref="CalculationContext.Outputs"/>. Must be zero-alloc.
        /// </summary>
        void Calculate(in CalculationContext context);
    }
}
