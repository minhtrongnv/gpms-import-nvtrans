using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nvtrans
{
    public class ShipMapping
    {
        public string OrgStructureName { get; set; }

        public Guid ID { get; set; }

        [JsonProperty("MappingId")]
        public JToken MappingIdValue { get; set; }

        [JsonIgnore]
        public Guid? MappingId
        {
            get
            {
                if (MappingIdValue == null ||
                    MappingIdValue.Type == JTokenType.Null ||
                    MappingIdValue.Type == JTokenType.Boolean)
                {
                    return null;
                }

                Guid mappingId;

                if (Guid.TryParse(MappingIdValue.ToString(), out mappingId))
                {
                    return mappingId;
                }

                return null;
            }
        }

        [JsonIgnore]
        public bool HasMapping
        {
            get
            {
                return MappingId.HasValue;
            }
        }
    }

    public class ShipMappingRepository
    {
        private readonly List<ShipMapping> _items;
        private readonly Dictionary<Guid, ShipMapping> _itemsById;

        public ShipMappingRepository(string filePath)
        {
            _items = LoadFromFile(filePath);
            _itemsById = new Dictionary<Guid, ShipMapping>();

            foreach (ShipMapping item in _items)
            {
                // Prevent duplicate ID errors.
                _itemsById[item.ID] = item;
            }
        }

        private List<ShipMapping> LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is empty.", "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Ship mapping file was not found.",
                    filePath
                );
            }

            string json = File.ReadAllText(filePath);

            List<ShipMapping> result =
                JsonConvert.DeserializeObject<List<ShipMapping>>(json);

            return result ?? new List<ShipMapping>();
        }

        public List<ShipMapping> GetAll()
        {
            return new List<ShipMapping>(_items);
        }

        public ShipMapping GetById(Guid id)
        {
            ShipMapping item;

            if (_itemsById.TryGetValue(id, out item))
            {
                return item;
            }

            return null;
        }

        public ShipMapping GetById(string id)
        {
            Guid guid;

            if (!Guid.TryParse(id, out guid))
            {
                return null;
            }

            return GetById(guid);
        }

        public Guid? GetMappingId(string id)
        {
            ShipMapping item = GetById(id);

            if (item == null)
            {
                return null;
            }

            return item.MappingId;
        }
    }
}