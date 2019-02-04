﻿// Generated by Xamasoft JSON Class Generator
// http://www.xamasoft.com/json-class-generator

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FimSync_Ezma.PersonDiscoveryJsonTypes;
using System.Collections;
using System.Reflection;

namespace FimSync_Ezma.PersonDiscoveryJsonTypes
{

    internal class Result : IEnumerable 
    {
        [JsonProperty("popularity")]
        public double popularity { get; set; }

        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("profile_path")]
        public string profile_path { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("known_for")]
        public KnownFor[] known_for { get; set; }

        [JsonProperty("adult")]
        public bool adult { get; set; }

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(Result);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
        }

        public IEnumerator GetEnumerator()
        {
            Type myType = typeof(Result);
            PropertyInfo[] myProps = myType.GetProperties();
            return myProps.GetEnumerator();
        }
    }

}