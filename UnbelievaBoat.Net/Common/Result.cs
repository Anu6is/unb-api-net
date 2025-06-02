using System;
using System.Diagnostics.CodeAnalysis; // For NotNullWhen attribute

namespace UnbelievaBoat.Net.Common
{
    public record Result<TSuccess, TError>
    {
        private readonly TSuccess _value;
        private readonly TError _error;

        public bool IsSuccess { get; }

        [MemberNotNullWhen(false, nameof(Error))]
        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsSuccessValue => IsSuccess; // Helper for pattern matching

        public TSuccess Value => IsSuccess ? _value : throw new InvalidOperationException("Result does not contain a success value.");
        public TError Error => !IsSuccess ? _error : throw new InvalidOperationException("Result does not contain an error value.");

        // Private constructor, use static factory methods
        private Result(TSuccess value)
        {
            IsSuccess = true;
            _value = value;
            _error = default; // Should not be accessed
        }

        private Result(TError error)
        {
            IsSuccess = false;
            _value = default; // Should not be accessed
            _error = error;
        }

        public static Result<TSuccess, TError> Success(TSuccess value) => new Result<TSuccess, TError>(value);
        public static Result<TSuccess, TError> Failure(TError error) => new Result<TSuccess, TError>(error);

        // Optional: implicit conversions for convenience
        public static implicit operator Result<TSuccess, TError>(TSuccess value) => Success(value);
        public static implicit operator Result<TSuccess, TError>(TError error) => Failure(error);

        // Optional: Match method for functional style error handling
        public TResult Match<TResult>(Func<TSuccess, TResult> success, Func<TError, TResult> failure)
        {
            return IsSuccess ? success(Value) : failure(Error);
        }
    }
}
