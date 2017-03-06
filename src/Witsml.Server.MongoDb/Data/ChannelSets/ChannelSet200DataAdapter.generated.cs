﻿//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

// ----------------------------------------------------------------------
// <auto-generated>
//     Changes to this file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Datatypes;
using LinqToQuerystring;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;

namespace PDS.Witsml.Server.Data.ChannelSets
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSet" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{ChannelSet}" />
    [Export(typeof(IWitsmlDataAdapter<ChannelSet>))]
    [Export200(ObjectTypes.ChannelSet, typeof(IWitsmlDataAdapter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ChannelSet200DataAdapter : MongoDbDataAdapter<ChannelSet>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200DataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelDataChunkAdapter">The channel data chunk adapter.</param>
        [ImportingConstructor]
        public ChannelSet200DataAdapter(IContainer container, IDatabaseProvider databaseProvider, ChannelDataChunkAdapter channelDataChunkAdapter)
            : base(container, databaseProvider, ObjectNames.ChannelSet200, ObjectTypes.Uuid)
        {
            Logger.Debug("Instance created.");
            ChannelDataChunkAdapter = channelDataChunkAdapter;
        }

        /// <summary>
        /// Gets the channel data chunk adapter.
        /// </summary>
        public ChannelDataChunkAdapter ChannelDataChunkAdapter { get; }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<ChannelSet> GetAll(EtpUri? parentUri)
        {
            Logger.DebugFormat("Fetching all ChannelSets; Parent URI: {0}", parentUri);

            return GetAllQuery(parentUri)
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{ChannelSet}" /> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected override IQueryable<ChannelSet> GetAllQuery(EtpUri? parentUri)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var uidWellbore = parentUri.Value.ObjectId;

                if (!string.IsNullOrWhiteSpace(uidWellbore))
                    query = query.Where(x => x.Wellbore.Uuid == uidWellbore);

                if (!string.IsNullOrWhiteSpace(parentUri.Value.Query))
                    query = query.LinqToQuerystring(parentUri.Value.Query);
            }

            return query;
        }
    }
}
