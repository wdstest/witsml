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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess.WITSML141;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Rigs
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="Rig"/>.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.WitsmlDataProvider{RigList, Rig}" />
    [Export(typeof(IEtpDataProvider))]
    [Export(typeof(IEtpDataProvider<Rig>))]
    [Export141(ObjectTypes.Rig, typeof(IEtpDataProvider))]
    [Export141(ObjectTypes.Rig, typeof(IWitsmlDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Rig141DataProvider : WitsmlDataProvider<RigList, Rig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rig141DataProvider"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        [ImportingConstructor]
        public Rig141DataProvider(IContainer container, IWitsmlDataAdapter<Rig> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetDefaultValues(Rig dataObject)
        {
            dataObject.Uid = dataObject.NewUid();
            dataObject.CommonData = dataObject.CommonData.Create();
        }

        /// <summary>
        /// Creates an <see cref="RigList" /> instance containing the specified data objects.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        /// <returns>The <see cref="RigList" /> instance.</returns>
        protected override RigList CreateCollection(List<Rig> dataObjects)
        {
            return new RigList { Rig = dataObjects };
        }
    }
}
