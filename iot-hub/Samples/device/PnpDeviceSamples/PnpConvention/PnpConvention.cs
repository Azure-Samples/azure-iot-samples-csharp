using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PnpHelpers
{
    public class PnpConvention
    {
        // The following regex expression minifies a json string.
        // It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        // The first option @"(""(?:[^""\\]|\\.)*"")" matches a double quoted string.
        // The "(?:[^""\\])" indicates that the output (within quotes) is captured, and available as replacement in the Regex.Replace call below.
        // The "[^""\\]" matches any character except a double quote or escape character \.
        // The second option "\s+" matches all other space characters.
        private static readonly Regex s_trimWhiteSpace = new Regex(@"(""(?:[^""\\]|\\.)*"")|\s+", RegexOptions.Compiled);

        /// <summary>
        /// The content type for a plug and play compatible telemetry message.
        /// </summary>
        public const string ContentApplicationJson = "application/json";

        /// <summary>
        /// The key for a component identifier within a property update patch. Corresponding value is <see cref="PropertyComponentIdentifierValue"/>.
        /// </summary>
        public const string PropertyComponentIdentifierKey = "__t";

        /// <summary>
        /// The value for a component identifier within a property update patch. Corresponding key is <see cref="PropertyComponentIdentifierKey"/>.
        /// </summary>
        public const string PropertyComponentIdentifierValue = "c";

        /// <summary>
        /// Create a plug and play compatible telemetry message.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="telemetryValue">The unserialized telemetry payload, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub. The caller must dispose this object when finished.</returns>
        public static Message CreateMessage(string telemetryName, object telemetryValue, string componentName = default, Encoding encoding = default)
        {
            if (string.IsNullOrWhiteSpace(telemetryName))
            {
                throw new ArgumentNullException(nameof(telemetryName));
            }
            if (telemetryValue == null)
            {
                throw new ArgumentNullException(nameof(telemetryValue));
            }

            return CreateMessage(new Dictionary<string, object> { { telemetryName, telemetryValue } }, componentName, encoding);
        }

        /// <summary>
        /// Create a plug and play compatible telemetry message.
        /// </summary>
        /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <param name="telemetryPairs">The unserialized name and value telemetry pairs, as defined in the DTDL interface. Names must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub. The caller must dispose this object when finished.</returns>
        public static Message CreateMessage(IDictionary<string, object> telemetryPairs, string componentName = default, Encoding encoding = default)
        {
            if (telemetryPairs == null)
            {
                throw new ArgumentNullException(nameof(telemetryPairs));
            }

            Encoding messageEncoding = encoding ?? Encoding.UTF8;
            string payload = JsonConvert.SerializeObject(telemetryPairs);
            var message = new Message(messageEncoding.GetBytes(payload))
            {
                ContentEncoding = messageEncoding.WebName,
                ContentType = ContentApplicationJson,
            };

            if (!string.IsNullOrWhiteSpace(componentName))
            {
                message.ComponentName = componentName;
            }

            return message;
        }

        /// <summary>
        /// Creates a batch property update payload for the specified property key/value pairs.
        /// </summary>
        /// <param name="propertyName">The name of the twin property.</param>
        /// <param name="propertyValue">The unserialized value of the twin property.</param>
        /// <returns>A compact payload of the properties to update.</returns>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a root-level property update, the patch is in the format: <c>{ "samplePropertyName": 20 }</c>
        /// </remarks>
        public static string CreatePropertyPatch(string propertyName, object propertyValue)
        {
            return CreatePropertyPatch(new Dictionary<string, object> { { propertyName, propertyValue } });
        }

        /// <summary>
        /// Creates a batch property update payload for the specified property key/value pairs/
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a root-level property update, the patch is in the format: <c>{ "samplePropertyName": 20 }</c>
        /// </remarks>
        /// <param name="propertyKeyValuePairs">The twin properties and values to update.</param>
        /// <returns>A compact payload of the properties to update.</returns>
        public static string CreatePropertyPatch(IDictionary<string, object> propertyKeyValuePairs)
        {
            return JsonConvert.SerializeObject(propertyKeyValuePairs);
        }

        /// <summary>
        /// Create a key/value property patch for updating digital twin properties.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName"": 20
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <param name="propertyName">The name of the twin property.</param>
        /// <param name="propertyValue">The unserialized value of the twin property.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static string CreateComponentPropertyPatch(string componentName, string propertyName, object propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            if (propertyValue == null)
            {
                throw new ArgumentNullException(nameof(propertyValue));
            }

            return CreateComponentPropertyPatch(componentName, new Dictionary<string, object> { { propertyName, propertyValue } });
        }

        /// <summary>
        /// Create a key/value property patch for updating digital twin properties.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName"": 20
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <param name="propertyKeyValuePairs">The property name and an unserialized value, as defined in the DTDL interface.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static string CreateComponentPropertyPatch(string componentName, IDictionary<string, object> propertyKeyValuePairs)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }
            if (propertyKeyValuePairs == null)
            {
                throw new ArgumentNullException(nameof(propertyKeyValuePairs));
            }

            var propertyPatch = new StringBuilder();
            propertyPatch.Append("{");
            propertyPatch.Append($"\"{componentName}\":");
            propertyPatch.Append("{");
            propertyPatch.Append($"\"{PropertyComponentIdentifierKey}\":\"{PropertyComponentIdentifierValue}\",");
            foreach (var kvp in propertyKeyValuePairs)
            {
                propertyPatch.Append($"\"{kvp.Key}\": {JsonConvert.SerializeObject(kvp.Value)},");
            }

            // remove the extra comma
            propertyPatch.Remove(propertyPatch.Length - 1, 1);
             
            propertyPatch.Append("}}");

            return propertyPatch.ToString();
        }

        /// <summary>
        /// Create a key-embedded value property patch for updating device properties. Embedded value property updates are
        /// sent from a device in response to a service-initiated read-write property update.
        /// </summary>
        /// <remarks>
        /// A property is either read-only or read-write from a service perspective. All properties are read-write from a
        /// device's perspective. For a root-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "samplePropertyName": {
        ///     "value": 20,
        ///     "ac": 200,
        ///     "av": 5,
        ///     "ad": "The update was successful."
        ///   }
        /// }
        /// </code>
        ///
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName": {
        ///       "value": 20,
        ///       "ac": 200,
        ///       "av": 5,
        ///       "ad": "The update was successful."
        ///     }
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="unserializedPropertyValue">The unserialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="ackCode">The acknowledgment code from the device, for the embedded value property update.</param>
        /// <param name="ackVersion">The version no. of the service-initiated read-write property update.</param>
        /// <param name="unserializedAckDescription">The serialized description from the device, accompanying the embedded value property update.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>The property patch for embedded value property updates for read-write properties.</returns>
        public static string CreatePropertyEmbeddedValuePatch(
            string propertyName,
            object unserializedPropertyValue,
            int ackCode,
            long ackVersion,
            string unserializedAckDescription = default,
            string componentName = default)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            if (unserializedPropertyValue == null)
            {
                throw new ArgumentNullException(nameof(unserializedPropertyValue));
            }

            string propertyPatch;
            if (string.IsNullOrWhiteSpace(componentName))
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\" : {JsonConvert.SerializeObject(unserializedPropertyValue)}," +
                    $"          \"ac\" : {ackCode}, " +
                    $"          \"av\" : {ackVersion}, " +
                    $"          {(!string.IsNullOrWhiteSpace(unserializedAckDescription) ? $"\"ad\": {JsonConvert.SerializeObject(unserializedAckDescription)}" : "")}" +
                    $"      }} " +
                    $"}}";
            }
            else
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{componentName}\": " +
                    $"      {{" +
                    $"          \"{PropertyComponentIdentifierKey}\": \"{PropertyComponentIdentifierValue}\"," +
                    $"          \"{propertyName}\": " +
                    $"              {{ " +
                    $"                  \"value\" : {JsonConvert.SerializeObject(unserializedPropertyValue)}," +
                    $"                  \"ac\" : {ackCode}, " +
                    $"                  \"av\" : {ackVersion}, " +
                    $"                  {(!string.IsNullOrWhiteSpace(unserializedAckDescription) ? $"\"ad\": {JsonConvert.SerializeObject(unserializedAckDescription)}" : "")}" +
                    $"              }} " +
                    $"      }} " +
                    $"}}";
            }

            return TrimWhiteSpace(propertyPatch);
        }

        /// <summary>
        /// Helper to retrieve the property value from the <see cref="TwinCollection"/> property update patch which was received as a result of service-initiated update.
        /// </summary>
        /// <typeparam name="T">The data type of the property, as defined in the DTDL interface.</typeparam>
        /// <param name="collection">The <see cref="TwinCollection"/> property update patch received as a result of service-initiated update.</param>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="propertyValue">The corresponding property value.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>A boolean indicating if the <see cref="TwinCollection"/> property update patch received contains the property update.</returns>
        public static bool TryGetPropertyFromTwin<T>(TwinCollection collection, string propertyName, out T propertyValue, string componentName = null)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // If the desired property update is for a root component or nested component, verify that property patch received contains the desired property update.
            propertyValue = default;

            if (string.IsNullOrWhiteSpace(componentName))
            {
                if (collection.Contains(propertyName))
                {
                    propertyValue = (T)collection[propertyName];
                    return true;
                }
            }

            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                if (componentProperty.ContainsKey(propertyName))
                {
                    propertyValue = componentProperty.Value<T>(propertyName);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Helper to remove extra white space from the supplied string.
        /// It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        /// </summary>
        /// <param name="input">The string to be formatted.</param>
        /// <returns>The input string, with extra white space removed. </returns>
        private static string TrimWhiteSpace(string input)
        {
            return s_trimWhiteSpace.Replace(input, "$1");
        }
    }
}
