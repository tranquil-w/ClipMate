// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/MahApps/MahApps.Metro.git

using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace ClipMate.Tests.TestHelpers;

public class SwitchContextToUiThreadAwaiter(Dispatcher uiContext) : INotifyCompletion
{
    public SwitchContextToUiThreadAwaiter GetAwaiter()
    {
        return this;
    }

    public bool IsCompleted => false;

    public void OnCompleted(Action continuation)
    {
        uiContext.Invoke(new Action(continuation));
    }

    public void GetResult()
    {
    }
}