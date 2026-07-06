using System;

namespace ExampleProject.Framework.Utils
{
    public class NoAvailableCellsException : Exception
    {
        public NoAvailableCellsException(string message) : base(message)
        {
        }
    }
}
