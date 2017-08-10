using System;
using System.Collections.Generic;
using System.Diagnostics;
using MongoDB.Driver;

namespace DomainFinder {

    /// <summary> 
    /// </summary>
    public class MongoDbHelper {
        private static MongoDbHelper _Instance = new MongoDbHelper();
        private static MongoClient _Client;

        public MongoDbHelper() {
            if (null == _Client) {
                try {
                    var connStr = Config.MongoDbConn;
                    if (!string.IsNullOrEmpty(connStr)) {
                        _Client = new MongoClient(connStr);
                    }
                } catch (Exception ex) {
                }
            }
        }

        /// <summary>
        /// 全局单例
        /// </summary>
        public static MongoDbHelper Instance {
            get {
                if (_Instance == null) {
                    _Instance = new MongoDbHelper();
                }
                return _Instance;
            }
        }

        public void ReConstruct() {
            _Instance = null;
        }

        public IMongoDatabase GetDb(string dbName = "") {
            IMongoDatabase db = null;
            try {
                db = _Client.GetDatabase(dbName);
            } catch (Exception ex) {
                ReConstruct();                
            }

            return db;
        }
    }
}