// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ImportExportDevicesWithManagedIdentitySample
{
    /// <summary>
    /// The type for managed identity to use in import and export jobs.
    /// </summary>
    public enum ManagedIdentityType
    {
        SystemDefined,
        UserDefined
    }
}
