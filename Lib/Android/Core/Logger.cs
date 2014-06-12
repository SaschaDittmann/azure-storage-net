﻿//-----------------------------------------------------------------------
// <copyright file="Logger.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics;

    internal static partial class Logger
    {
        internal static void LogError(OperationContext operationContext, string format, params object[] args)
        {
            if (Logger.ShouldLog(LogLevel.Error, operationContext))
            {
                Debug.WriteLine(Logger.FormatLine(operationContext, format, args));
            }
        }

        internal static void LogWarning(OperationContext operationContext, string format, params object[] args)
        {
            if (Logger.ShouldLog(LogLevel.Warning, operationContext))
            {
                Debug.WriteLine(Logger.FormatLine(operationContext, format, args));
            }
        }

        internal static void LogInformational(OperationContext operationContext, string format, params object[] args)
        {
            if (Logger.ShouldLog(LogLevel.Informational, operationContext))
            {
                Debug.WriteLine(Logger.FormatLine(operationContext, format, args));
            }
        }

        internal static void LogVerbose(OperationContext operationContext, string format, params object[] args)
        {
            if (Logger.ShouldLog(LogLevel.Verbose, operationContext))
            {
                Debug.WriteLine(Logger.FormatLine(operationContext, format, args));
            }
        }
    }
}
