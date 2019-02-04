using System;
using Microsoft.MetadirectoryServices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace FimSync_Ezma
{
    public class EzmaExtension :
    IMAExtensible2CallImport,
    //IMAExtensible2CallExport,
    //IMAExtensible2FileImport,
    //IMAExtensible2FileExport,
    //IMAExtensible2GetHierarchy,
    IMAExtensible2GetSchema,
    IMAExtensible2GetCapabilities,
    IMAExtensible2GetParameters
    //IMAExtensible2GetPartitions
    {
        //
        // Constructor
        //
        public EzmaExtension()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region Variables/Constants
        //Constants
        private const string CS_OBJECTTYPE_PERSON = "person";
        private const string CS_OBJECTTYPE_MOVIE = "movie";

        //Import
        private string _lastRunTimeStamp;
        private string _currentRuntimeStamp;
        private List<dynamic> _personDiscoveryList;
        private List<MovieDiscoveryJsonTypes.Result> _moviesList;
        private int _currentPageSize;
        private int _objectCount;
        private bool _skipPeople;

        //Operators
        private RESTHandler _peopleDiscovery;
        private readonly string _BASE_URI = "Base URI";
        private readonly string _API_KEY = "API Key";
        private readonly string _LANG = "Language";
        private readonly string _NOPAGESRTN = "# of Pages to Return";

        public int ImportDefaultPageSize { get; } = 1000;

        public int ImportMaxPageSize { get; } = 2000;
        #endregion

        #region Import

        public OpenImportConnectionResults OpenImportConnection(KeyedCollection<string, ConfigParameter> configParameters, Schema types, OpenImportConnectionRunStep importRunStep)
        {
            OpenImportConnectionResults openImportConnectionResults = new OpenImportConnectionResults();
            _lastRunTimeStamp = openImportConnectionResults.CustomData = importRunStep.CustomData;
            _currentRuntimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _currentPageSize = importRunStep.PageSize;
            try
            {
                _lastRunTimeStamp = openImportConnectionResults.CustomData = importRunStep.CustomData;
                _peopleDiscovery = new RESTHandler(configParameters[_API_KEY].Value,
                    configParameters[_BASE_URI].Value,
                    configParameters[_LANG].Value,
                    configParameters[_NOPAGESRTN].Value);
                _personDiscoveryList = _peopleDiscovery.ReadObjects();
                _moviesList = new List<MovieDiscoveryJsonTypes.Result>();
                _objectCount = 0;
                _skipPeople = false;
            }
            catch (Exception ex)
            {
                LogEvent(ex.ToString());
                throw new ExtensibleExtensionException(ex.ToString());
            }
            return openImportConnectionResults;
        }

        public GetImportEntriesResults GetImportEntries(GetImportEntriesRunStep importRunStep)
        {
            _lastRunTimeStamp = importRunStep.CustomData;
            GetImportEntriesResults importReturnInfo = new GetImportEntriesResults();
            List<CSEntryChange> csentries = new List<CSEntryChange>();

            if (!_skipPeople)
            {
                //foreach (JObject person in _personDiscoveryList)
                for (int peepsPos = _currentPeoplePos; peepsPos < _personDiscoveryList.Count; peepsPos++)
                {
                    CSEntryChange csentryPerson = CSEntryChange.Create();
                    csentryPerson.ObjectType = CS_OBJECTTYPE_PERSON;
                    csentryPerson.ObjectModificationType = ObjectModificationType.Add;
                    Dictionary<string, JToken> result = _personDiscoveryList[peepsPos].ToObject<Dictionary<string, JToken>>();
                    foreach (KeyValuePair<string, JToken> item in result)
                    {
                        if (item.Key.ToLower().Equals("id"))
                        {
                            string itemName = string.Format("{0}_{1}", CS_OBJECTTYPE_PERSON, item.Key);
                            string itemValue = string.Format("{0}_{1}", CS_OBJECTTYPE_PERSON, item.Value.ToString());
                            csentryPerson.AnchorAttributes.Add(AnchorAttribute.Create(itemName, itemValue));
                        }
                        else if (item.Key.ToLower().Equals("known_for") && item.Value.GetType().Name.Equals("JArray"))
                        {
                            if (item.Value.HasValues)
                            {
                                List<object> knowFor = new List<object>();
                                foreach (var movieObject in item.Value)
                                {
                                    MovieDiscoveryJsonTypes.Result movie = new MovieDiscoveryJsonTypes.Result();
                                    foreach (JToken itemValue in movieObject.Children())
                                    {
                                        string movieAnchor = string.Empty;
                                        if (((JProperty)itemValue).Name.Equals("id"))
                                        {
                                            movieAnchor = string.Format("{0}_{1}", CS_OBJECTTYPE_MOVIE, ((JProperty)itemValue).Value);
                                            knowFor.Add(movieAnchor);
                                            movie.id = ((int)((JProperty)itemValue).Value);
                                        }
                                        else if (((JProperty)itemValue).Name.Equals("genre_ids") ||
                                            ((JProperty)itemValue).Name.Equals("origin_country"))
                                        { }
                                        else
                                        {
                                            movie[((JProperty)itemValue).Name] = ((JValue)((JProperty)itemValue).Value).Value;
                                        }
                                    }
                                    bool addMovie = true;
                                    for (int i = 0; i < _moviesList.Count; i++)
                                    {
                                        if (_moviesList[i].id.Equals(movie.id))
                                        {
                                            addMovie = false;
                                            break;
                                        }
                                    }
                                    if (addMovie)
                                    { _moviesList.Add(movie); }
                                }
                                csentryPerson.AttributeChanges.Add(AttributeChange.CreateAttributeAdd(item.Key, knowFor));
                            }
                        }
                        else
                        {
                            csentryPerson.AttributeChanges.Add(AttributeChange.CreateAttributeAdd(item.Key, item.Value.ToString()));
                        }
                    }
                    csentries.Add(csentryPerson);
                    if (peepsPos >= _currentPageSize)
                    {
                        importReturnInfo.MoreToImport = true;
                        _currentPeoplePos = peepsPos;
                        break;
                    }
                }
            }

            if (!importReturnInfo.MoreToImport)
            {
                //foreach (MovieDiscoveryJsonTypes.Result movie in _moviesList)
                for (int moviesPos = _currentMoviePos; moviesPos < _moviesList.Count; moviesPos++)
                {
                    string anchor = "id";

                    CSEntryChange csentryMovie = CSEntryChange.Create();
                    csentryMovie.ObjectType = CS_OBJECTTYPE_MOVIE;
                    csentryMovie.ObjectModificationType = ObjectModificationType.Add;

                    foreach (PropertyInfo property in _moviesList[moviesPos])
                    {
                        if (property.Name.Equals("id"))
                        {
                            string propertyName = string.Format("{0}_{1}", CS_OBJECTTYPE_MOVIE, anchor);
                            string propertyValue = string.Format("{0}_{1}", CS_OBJECTTYPE_MOVIE, _moviesList[moviesPos][anchor].ToString());
                            csentryMovie.AnchorAttributes.Add(AnchorAttribute.Create(propertyName, propertyValue));
                        }
                        else if (property.Name.ToLower().Equals("item")) { }
                        else if (property.Name != null)
                        {
                            csentryMovie.AttributeChanges.Add(AttributeChange.CreateAttributeAdd(property.Name, property.Name));
                        }
                    }
                    csentries.Add(csentryMovie);
                    if (_currentPeoplePos >= _personDiscoveryList.Count)
                    {
                        if (_currentMoviePos >= _currentPageSize)
                        {
                            importReturnInfo.MoreToImport = true;
                            break;
                        }
                    }
                    else
                    {
                        if (_currentMoviePos + _currentPeoplePos >= _currentPageSize)
                        {
                            _skipPeople = true;
                            importReturnInfo.MoreToImport = true;
                            break;
                        }
                    }
                }
            }

            importReturnInfo.CSEntries = csentries;
            return importReturnInfo;
        }

        public CloseImportConnectionResults CloseImportConnection(CloseImportConnectionRunStep importRunStep)
        {
            CloseImportConnectionResults closeImportConnectionResults = new CloseImportConnectionResults();
            closeImportConnectionResults.CustomData = _currentRuntimeStamp;
            return closeImportConnectionResults;
        }
        #endregion

        #region ECMAUtils
        public static void LogEvent(string message)
        {
            string sSource = "Movie ECMA2";
            string sLog = "Application";
            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);
            EventLog.WriteEntry(sSource, message);
        }

        public MACapabilities Capabilities
        {
            get
            {
                MACapabilities myCapabilities = new MACapabilities();

                myCapabilities.ConcurrentOperation = true;
                myCapabilities.ObjectRename = false;
                myCapabilities.DeleteAddAsReplace = true;
                myCapabilities.DeltaImport = false;
                myCapabilities.DistinguishedNameStyle = MADistinguishedNameStyle.None;
                myCapabilities.ExportType = MAExportType.AttributeUpdate;
                myCapabilities.NoReferenceValuesInFirstExport = true;
                myCapabilities.Normalizations = MANormalizations.None;

                return myCapabilities;
            }
        }

        public Schema GetSchema(KeyedCollection<string, ConfigParameter> configParameters)
        {
            //TODO: Refactor object discover to a function
            Schema schema = Schema.Create();
            // person
            SchemaType personType = Microsoft.MetadirectoryServices.SchemaType.Create(CS_OBJECTTYPE_PERSON, false);
            PersonDiscoveryJsonTypes.Result personSchema = new PersonDiscoveryJsonTypes.Result();
            // movie
            SchemaType movieType = Microsoft.MetadirectoryServices.SchemaType.Create(CS_OBJECTTYPE_MOVIE, false);
            MovieDiscoveryJsonTypes.Result movieSchema = new MovieDiscoveryJsonTypes.Result();
            foreach (PropertyInfo item in personSchema)
            {
                if (item.Name.ToLower().Equals("id"))
                {
                    string itemName = string.Format("{0}_{1}", CS_OBJECTTYPE_PERSON, item.Name);
                    personType.Attributes.Add(SchemaAttribute.CreateAnchorAttribute(itemName, AttributeType.String));
                }
                else if (item.PropertyType.IsArray)
                {
                    switch (item.Name.ToLower())
                    {
                        case "known_for":
                            personType.Attributes.Add(SchemaAttribute.CreateMultiValuedAttribute(item.Name, AttributeType.Reference));
                            break;
                        default:
                            personType.Attributes.Add(SchemaAttribute.CreateMultiValuedAttribute(item.Name, AttributeType.String));
                            break;
                    }
                }
                else if (item.Name.ToLower().Equals("item")) { }
                else
                {
                    personType.Attributes.Add(SchemaAttribute.CreateSingleValuedAttribute(item.Name, AttributeType.String));
                }
            }

            foreach (PropertyInfo item in movieSchema)
            {
                if (item.Name.ToLower().Equals("id"))
                {
                    string itemName = string.Format("{0}_{1}", CS_OBJECTTYPE_MOVIE, item.Name);
                    movieType.Attributes.Add(SchemaAttribute.CreateAnchorAttribute(itemName, AttributeType.String));
                }
                else if (item.PropertyType.IsArray)
                {
                    switch (item.Name.ToLower())
                    {
                        default:
                            movieType.Attributes.Add(SchemaAttribute.CreateMultiValuedAttribute(item.Name, AttributeType.String));
                            break;
                    }
                }
                else if (item.Name.ToLower().Equals("item")) { }
                else
                {
                    movieType.Attributes.Add(SchemaAttribute.CreateSingleValuedAttribute(item.Name, AttributeType.String));
                }
            }

            schema.Types.Add(personType);
            schema.Types.Add(movieType);
            return schema;
        }

        public IList<ConfigParameterDefinition> GetConfigParameters(KeyedCollection<string, ConfigParameter> configParameters, ConfigParameterPage page)
        {
            List<ConfigParameterDefinition> configParametersDefinitions = new List<ConfigParameterDefinition>();

            switch (page)
            {
                case ConfigParameterPage.Connectivity:
                    configParametersDefinitions.Add(ConfigParameterDefinition.CreateStringParameter(_BASE_URI, "", "https://api.themoviedb.org/3/"));
                    configParametersDefinitions.Add(ConfigParameterDefinition.CreateStringParameter(_API_KEY, ""));
                    configParametersDefinitions.Add(ConfigParameterDefinition.CreateStringParameter(_LANG, "", "en-GB"));
                    configParametersDefinitions.Add(ConfigParameterDefinition.CreateStringParameter(_NOPAGESRTN, "", "40"));
                    break;
                default:
                    break;
            }
            return configParametersDefinitions;
        }

        public ParameterValidationResult ValidateConfigParameters(KeyedCollection<string, ConfigParameter> configParameters, ConfigParameterPage page)
        {
            ParameterValidationResult myResults = new ParameterValidationResult();
            return myResults;
        }
        #endregion

    };
}
