using System;

namespace HireHub.API.Exceptions
{
    public class DuplicateEmailException : Exception
    {
        public DuplicateEmailException(string? message = null) : base(message ?? "Duplicate email") { }
    }
}
