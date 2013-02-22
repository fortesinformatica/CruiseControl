﻿namespace CruiseControl.Core.Tests.Stubs
{
    using System;
    using CruiseControl.Core.Interfaces;

    public class TaskFailureActionStub
        : TaskFailureAction
    {
        public Action<IValidationLog> OnValidate { get; set; }
        public override void Validate(IValidationLog validationLog)
        {
            base.Validate(validationLog);
            if (this.OnValidate != null)
            {
                this.OnValidate(validationLog);
            }
        }
    }
}
