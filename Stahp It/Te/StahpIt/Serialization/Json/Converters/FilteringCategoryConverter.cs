/*
* Copyright (c) 2016 Jesse Nicholson.
*
* This file is part of Stahp It.
*
* Stahp It is free software: you can redistribute it and/or
* modify it under the terms of the GNU General Public License as published
* by the Free Software Foundation, either version 3 of the License, or (at
* your option) any later version.
*
* In addition, as a special exception, the copyright holders give
* permission to link the code of portions of this program with the OpenSSL
* library.
*
* You must obey the GNU General Public License in all respects for all of
* the code used other than OpenSSL. If you modify file(s) with this
* exception, you may extend this exception to your version of the file(s),
* but you are not obligated to do so. If you do not wish to do so, delete
* this exception statement from your version. If you delete this exception
* statement from all source files in the program, then also delete it
* here.
*
* Stahp It is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
* Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with Stahp It. If not, see <http://www.gnu.org/licenses/>.
*/

using ByteSizeLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Te.HttpFilteringEngine;
using Te.StahpIt.Filtering;

namespace Te.StahpIt.Serialization.Json.Converters
{
    internal class FilteringCategoryConverter : JsonConverter
    {
        private Engine m_engine;

        /// <summary>
        /// JSON Converter for the FilteringCategory object.
        /// </summary>
        /// <param name="engine">
        /// A valid instance to the underlying Engine that deserialized/serialized categories are
        /// associated with.
        /// </param>
        /// <exception cref="ArgumentException">
        /// In the event that the engine parameter is null, will throw ArgumentException.
        /// </exception>
        public FilteringCategoryConverter(Engine engine)
        {
            m_engine = engine;

            if (m_engine == null)
            {
                throw new ArgumentException("Expected valid instance of Engine");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(FilteringCategory));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            var cat = new FilteringCategory(m_engine);

            cat.CategoryName = jo["CategoryName"].ToObject<string>();
            cat.RuleSource = jo["RuleSource"].ToObject<Uri>();
            cat.TotalDataBlocked = ByteSize.FromBytes(jo["TotalDataBlocked"]["Bytes"].ToObject<double>());
            cat.TotalRequestsBlocked = jo["TotalRequestsBlocked"].ToObject<ulong>();
            cat.Enabled = jo["Enabled"].ToObject<bool>();

            return cat;
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}