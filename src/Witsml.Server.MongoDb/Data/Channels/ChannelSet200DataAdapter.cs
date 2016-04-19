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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSet" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{ChannelSet}" />
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    [Export(typeof(IEtpDataAdapter))]
    [Export(typeof(IEtpDataAdapter<ChannelSet>))]
    [Export200(ObjectTypes.ChannelSet, typeof(IEtpDataAdapter))]
    [Export200(ObjectTypes.ChannelSet, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelSet200DataAdapter : MongoDbDataAdapter<ChannelSet>, IChannelDataProvider
    {
        private readonly ChannelDataChunkAdapter _channelDataChunkAdapter;
        private readonly string _utcFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelDataChunkAdapter">The channel data chunk adapter.</param>
        [ImportingConstructor]
        public ChannelSet200DataAdapter(IDatabaseProvider databaseProvider, ChannelDataChunkAdapter channelDataChunkAdapter) : base(databaseProvider, ObjectNames.ChannelSet200, ObjectTypes.Uuid)
        {
            _channelDataChunkAdapter = channelDataChunkAdapter;
        }

        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(EtpUri uri)
        {
            var entity = GetEntity(uri);
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (entity.Channel == null || !entity.Channel.Any())
                return metadata;

            var indexMetadata = entity.Index
                .Select(x => ToIndexMetadataRecord(entity, x))
                .ToList();

            metadata.AddRange(entity.Channel.Select(x =>
            {
                var channel = ToChannelMetadataRecord(entity, x, indexMetadata);
                channel.ChannelId = index++;
                return channel;
            }));

            return metadata;
        }

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>A collection of channel data.</returns>
        public IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range)
        {
            var entity = GetEntity(uri);
            var indexChannel = entity.Index.FirstOrDefault();
            var increasing = indexChannel.Direction.GetValueOrDefault() == IndexDirection.increasing;
            var chunks = _channelDataChunkAdapter.GetData(uri, indexChannel.Mnemonic, range, increasing);
            return chunks.GetRecords(range, increasing);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<ChannelSet> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            //if (parentUri != null)
            //{
            //    var uidLog = parentUri.Value.ObjectId;
            //    query = query.Where(x => x.Log.Uuid == uidLog);
            //}

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Adds <see cref="ChannelSet"/> to the data store.
        /// </summary>
        /// <param name="entity">The <see cref="ChannelSet"/> to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(ChannelSet entity)
        {
            entity.Uuid = NewUid(entity.Uuid);
            entity.Channel.ForEach(c => c.Uuid = NewUid(c.Uuid));
            entity.Citation = entity.Citation.Create();
            Logger.DebugFormat("Adding ChannelSet with uid '{0}' and name '{1}'", entity.Uuid, entity.Citation.Title);

            Validate(Functions.AddToStore, entity);
            Logger.DebugFormat("Validated ChannelSet with uid '{0}' and name '{1}' for Add", entity.Uuid, entity.Citation.Title);

            // Extract Data
            var reader = ExtractDataReader(entity);

            InsertEntity(entity);

            if (reader != null)
            {
                Logger.DebugFormat("Adding log data with uid '{0}' and name '{1}'", entity.Uuid, entity.Citation.Title);
                var increasing = entity.Index.FirstOrDefault().Direction == IndexDirection.increasing;
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(entity);
                var indexCurve = entity.Index[0];
                Logger.DebugFormat("Index curve mnemonic: {0}.", indexCurve.Mnemonic);

                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Add(reader);

                // Update index range
                UpdateIndexRange(entity.GetUri(), entity, ranges, allMnemonics);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }

        /// <summary>
        /// Updates the specified <see cref="Log"/> instance in the store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<Log>();

            Logger.DebugFormat("Updating Log with uid '{0}'.", uri.ObjectId);
            //Validate(Functions.UpdateInStore, entity);

            var ignored = new[] { "Data" };
            UpdateEntity(parser, uri, ignored);

            // Extract Data
            var entity = Parse(parser.Context.Xml);
            var reader = ExtractDataReader(entity, GetEntity(uri));

            UpdateChannelDataAndIndexRange(uri, reader);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            UpdateChannelDataAndIndexRange(uri, reader);
            }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Delete(EtpUri uri)
        {
            var result = base.Delete(uri);

            if (result.Code == ErrorCodes.Success)
                result = _channelDataChunkAdapter.Delete(uri);

            return result;
        }

        internal ChannelDataReader ExtractDataReader(ChannelSet entity, ChannelSet existing = null)
        {
            // TODO: Handle: if (!string.IsNullOrEmpty(entity.Data.FileUri))
            // return null;

            if (existing == null)
            {
                var reader = entity.GetReader();
                entity.Data = null;
                return reader;
            }

            existing.Data = entity.Data;
            return existing.GetReader();
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelSet entity, Channel channel, IList<IndexMetadataRecord> indexMetadata)
        {
            var uri = channel.GetUri(entity);
            var primaryIndex = indexMetadata.FirstOrDefault();
            var isTimeLog = primaryIndex != null ? primaryIndex.IndexType == ChannelIndexTypes.Time : false;
            var curveIndexes = GetCurrentIndexRange(entity);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = channel.DataType.GetValueOrDefault(EtpDataType.@double).ToString().Replace("@", string.Empty),
                Description = channel.Citation != null ? channel.Citation.Description ?? channel.Mnemonic : channel.Mnemonic,
                Mnemonic = channel.Mnemonic,
                Uom = channel.UoM,
                MeasureClass = channel.CurveClass ?? ObjectTypes.Unknown,
                Source = channel.Source ?? ObjectTypes.Unknown,
                Uuid = channel.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                StartIndex = primaryIndex == null ? null : curveIndexes[channel.Mnemonic].Start.IndexToScale(primaryIndex.Scale, isTimeLog),
                EndIndex = primaryIndex == null ? null : curveIndexes[channel.Mnemonic].End.IndexToScale(primaryIndex.Scale, isTimeLog),
                Indexes = indexMetadata
            };
        }

        private IndexMetadataRecord ToIndexMetadataRecord(ChannelSet entity, ChannelIndex indexChannel, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexChannel.GetUri(entity),
                Mnemonic = indexChannel.Mnemonic,
                Description = indexChannel.Mnemonic,
                Uom = indexChannel.Uom,
                Scale = scale,
                IndexType = indexChannel.IndexType == ChannelIndexType.datetime || indexChannel.IndexType == ChannelIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = indexChannel.Direction == IndexDirection.decreasing
                    ? IndexDirections.Decreasing
                    : IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private void UpdateChannelDataAndIndexRange(EtpUri uri, ChannelDataReader reader)
        {
            // Get Updated ChannelSet
            var current = GetEntity(uri);

            // Merge ChannelDataChunks
            if (reader != null)
            {
                var increasing = current.Index.FirstOrDefault().Direction == IndexDirection.increasing;
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(current);
                var indexCurve = current.Index[0];

                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Merge(reader);

                // Update index range
                UpdateIndexRange(uri, current, ranges, allMnemonics);
            }
        }

        private Dictionary<string, Range<double?>> GetCurrentIndexRange(ChannelSet entity)
        {
            var ranges = new Dictionary<string, Range<double?>>();
            var index = entity.Index.FirstOrDefault();
            AddIndexRange(index.Mnemonic, entity.StartIndex, entity.EndIndex, ranges);

            foreach (var channel in entity.Channel)
            {
                AddIndexRange(channel.Mnemonic, channel.StartIndex, channel.EndIndex, ranges);
            }

            return ranges;
        }

        private void AddIndexRange(string mnemonic, AbstractIndexValue start, AbstractIndexValue end, Dictionary<string, Range<double?>> ranges)
        {
            var range = new List<double?> { null, null };

            double? startValue = null;
            double? endValue = null;

            if (start is TimeIndexValue)
            {
                var startTime = start as TimeIndexValue;
                if (startTime != null && !string.IsNullOrEmpty(startTime.Time))
                    startValue = DateTimeOffset.Parse(startTime.Time).ToUnixTimeSeconds();
                var endTime = end as TimeIndexValue;
                if (endTime != null && !string.IsNullOrEmpty(endTime.Time))
                    endValue = DateTimeOffset.Parse(endTime.Time).ToUnixTimeSeconds();
            }
            else if (start is DepthIndexValue)
            {
                var startDepth = start as DepthIndexValue;
                if (startDepth != null && startDepth.Depth.HasValue)
                    startValue = startDepth.Depth.Value;
                var endDepth = end as DepthIndexValue;
                if (endDepth != null && endDepth.Depth.HasValue)
                    endValue = endDepth.Depth.Value;
            }
            else
            {
                var startPass = start as PassIndexedDepth;
                if (startPass != null && startPass.Depth.HasValue)
                    startValue = startPass.Depth.Value;
                var endPass = end as PassIndexedDepth;
                if (endPass != null && endPass.Depth.HasValue)
                    endValue = endPass.Depth.Value;
            }
            ranges.Add(mnemonic, new Range<double?>(startValue, endValue));
        }

        private AbstractIndexValue UpdateIndexValue(ChannelIndexType? indexType, AbstractIndexValue current, double value)
        {
            AbstractIndexValue indexValue = null;

            if (indexType == ChannelIndexType.datetime || indexType == ChannelIndexType.elapsedtime)
            {
                if (current == null)
                    indexValue = new TimeIndexValue();
                else
                    indexValue = current;
                ((TimeIndexValue)indexValue).Time = DateTimeOffset.FromUnixTimeSeconds((long)value).ToString(_utcFormat);
            }
            else if (indexType == ChannelIndexType.passindexeddepth)
            {
                if (current == null)
                    indexValue = new PassIndexedDepth();
                else
                    indexValue = current;
                ((PassIndexedDepth)indexValue).Depth = (float)value;
            }
            else
            {
                if (current == null)
                    indexValue = new DepthIndexValue();
                else
                    indexValue = current;
                ((DepthIndexValue)indexValue).Depth = (float)value;
            }

            return indexValue;
        }

        private void GetUpdatedLogHeaderIndexRange(ChannelDataReader reader, string[] mnemonics, Dictionary<string, Range<double?>> ranges, bool increasing = true)
        {
            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                Range<double?> current;

                if (!ranges.TryGetValue(mnemonic, out current))
                    current = new Range<double?>(null, null);

                var update = reader.GetChannelIndexRange(i);
                var start = current.Start;
                var end = current.End;

                if (!current.Start.HasValue || !update.StartsAfter(current.Start.Value, increasing))
                    start = update.Start;
                if (!current.End.HasValue || !update.EndsBefore(current.End.Value, increasing))
                    end = update.End;

                ranges[mnemonic] = new Range<double?>(start, end);
            }
        }

        private void UpdateIndexRange(EtpUri uri, ChannelSet entity, Dictionary<string, Range<double?>> ranges, IEnumerable<string> mnemonics)
        {
            var mongoUpdate = new MongoDbUpdate<ChannelSet>(GetCollection(), null);

            var idField = MongoDbUtility.LookUpIdField(typeof(ChannelSet), "Uuid");

            var filter = MongoDbUtility.GetEntityFilter<ChannelSet>(uri, idField);
            UpdateDefinition<ChannelSet> channelIndexUpdate = null;

            if (entity.Citation != null)
            {
                if (entity.Citation.Creation.HasValue)
                {
                    var creationTime = entity.Citation.Creation;
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.Creation", creationTime.Value.ToString(_utcFormat));
                }
                if (entity.Citation.LastUpdate.HasValue)
                {
                    var updateTime = entity.Citation.LastUpdate;
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.LastUpdate", updateTime.Value.ToString(_utcFormat));
                }
            }

            var indexChannel = entity.Index.FirstOrDefault();
            var indexType = indexChannel.IndexType;
            var indexMnemonic = indexChannel.Mnemonic;
            var range = ranges[indexMnemonic];

            if (range.Start.HasValue)
            {
                var start = UpdateIndexValue(indexType, entity.StartIndex, range.Start.Value);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "StartIndex", start);
            }

            if (range.End.HasValue)
            {
                var end = UpdateIndexValue(indexType, entity.EndIndex, range.End.Value);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "EndIndex", end);
            }

            if (channelIndexUpdate != null)
                mongoUpdate.UpdateFields(filter, channelIndexUpdate);

            idField = MongoDbUtility.LookUpIdField(typeof(Channel), "Uuid");

            foreach (var mnemonic in mnemonics)
            {
                var channel = entity.Channel.FirstOrDefault(c => c.Mnemonic.EqualsIgnoreCase(mnemonic));
                if (channel == null)
                    continue;

                var channelFilter = Builders<ChannelSet>.Filter.And(filter,
                    MongoDbUtility.BuildFilter<ChannelSet>("Channel." + idField, channel.Uuid));

                UpdateDefinition<ChannelSet> updates = null;
                range = ranges[mnemonic];

                if (range.Start.HasValue)
                {
                    var start = UpdateIndexValue(indexType, channel.StartIndex, range.Start.Value);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.StartIndex", start);
                }

                if (range.End.HasValue)
                {
                    var end = UpdateIndexValue(indexType, channel.EndIndex, range.End.Value);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.EndIndex", end);
                }

                if (updates != null)
                {
                    mongoUpdate.UpdateFields(channelFilter, updates);
                }
            }
        }
    }
}
