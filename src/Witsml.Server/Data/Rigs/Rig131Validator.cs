﻿//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML131;

namespace PDS.Witsml.Server.Data.Rigs
{
    /// <summary>
    /// Provides validation for <see cref="Rig" /> data objects.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.DataObjectValidator{Rig}" />
    [Export(typeof(IDataObjectValidator<Rig>))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Rig131Validator : DataObjectValidator<Rig, Wellbore, Well>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rig131Validator"/> class.
        /// </summary>
        /// <param name="rigDataAdapter">The rig data adapter.</param>
        /// <param name="wellboreDataAdapter">The wellbore data adapter.</param>
        /// <param name="wellDataAdapter">The well data adapter.</param>
        [ImportingConstructor]
        public Rig131Validator(IWitsmlDataAdapter<Rig> rigDataAdapter, IWitsmlDataAdapter<Wellbore> wellboreDataAdapter, IWitsmlDataAdapter<Well> wellDataAdapter)
            : base(rigDataAdapter, wellboreDataAdapter, wellDataAdapter)
        {
        }
    }
}
