using System;

namespace GatherUp.Core.Exceptions
{
    public class NotFoundException : BusinessException
    {
        public NotFoundException() { }
        public NotFoundException(string message) : base(message) { }
    }
}
