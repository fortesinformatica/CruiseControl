﻿namespace CruiseControl.Core.Tests.Stubs
{
    using System;
    using CruiseControl.Core.Interfaces;

    public class ValidationLogStub
        : IValidationLog
    {
        public int NumberOfErrors
        {
            get { throw new NotImplementedException(); }
        }

        public int NumberOfWarnings
        {
            get { throw new NotImplementedException(); }
        }

        public Action<string, object[]> OnAddErrorMessage { get; set; }
        public void AddError(string message, params object[] args)
        {
            if (this.OnAddErrorMessage == null)
            {
                throw new NotImplementedException();
            }

            this.OnAddErrorMessage(message, args);
        }

        public void AddError(Exception error)
        {
            throw new NotImplementedException();
        }

        public Action<string, object[]> OnAddWarningMessage { get; set; }
        public void AddWarning(string message, params object[] args)
        {
            if (this.OnAddWarningMessage == null)
            {
                throw new NotImplementedException();
            }

            this.OnAddWarningMessage(message, args);
        }

        public void AddWarning(Exception error)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
