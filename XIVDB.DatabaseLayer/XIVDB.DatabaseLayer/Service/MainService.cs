﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XIVDB.DatabaseLayer.Access;
using XIVDB.DatabaseLayer.Global;
using XIVDB.DatabaseLayer.Static;
using XIVDB.Interfaces;

namespace XIVDB.DatabaseLayer.Service
{
    /// <summary>
    /// Service class used to interact with XIV Database
    /// </summary>
    internal class MainService
    {
        #region Private Members
        private static XivDbAccess _access;
        private readonly Logger _log;
        #endregion

        #region Constructors
        public MainService()
        {
            //Instantiate logger
            _log = new Logger(this);
            if (_access == null) _access = XivDbAccess.GetInstance();
            //Validate database
            ValidateDbStatus();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns an IEnumerable of results returned from the database
        /// </summary>
        /// <typeparam name="T">Type of object where T = IXivdbObject</typeparam>
        /// <param name="model">IXivdbObject with properties used to construct query</param>
        /// <returns>IEnumerable of IXivdbObjects returned from database</returns>
        public IEnumerable<T> Get<T>(IXivdbObject model) where T : IXivdbObject
        {
            return _access.Get<T>(model);
        } 
        #endregion

        #region Private Methods
        /// <summary>
        /// Reads Status property from XivDbAccess class to determine what to do
        /// </summary>
        private void ValidateDbStatus()
        {
            //Check dbStatus and take appropriate action
            switch (XivDbAccess.Status)
            {
                case DbStatus.TablesNotCreated: CreateDbTables();
                    break;
                case DbStatus.DatabaseFileNotFound: _log.Fatal("Fatal Error occured. Database File not found.");
                    break;
                case DbStatus.DatabaseConfigFileNotFound: _log.Info("DatabaseConfigFileNotFound status detected.");
                    break;
                case DbStatus.Ok: _log.Info("Database Status is OK.");
                    break;
                case DbStatus.Unknown: _log.Warning("Database Status is unknown...");
                    break;
                default:
                    _log.Warning("Unknown DbStatus value in switch statement...");
                    break;
            }
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Creates Database file
        /// </summary>
        internal void CreateDbFile()
        {
            //Log this process
            _log.Info("Database file not found. Creating file...");
            //Create directory first.
            System.IO.Directory.CreateDirectory(Data.Database.DirectoryName);
            //Create sqlite file
            System.IO.File.Create(Data.Database.FilePath);
            _log.Info("Database file created.");

            //Create tables
            CreateDbTables();
        }

        /// <summary>
        /// Scans the Assembly for the Model namespace to create DB tables
        /// </summary>
        internal void CreateDbTables()
        {
            //Foreach Model Type found in the Model namespace...
            foreach (var model 
                in Assembly.Load(Data.App.DataLayerAssembly).GetTypes()
                .Where(t => t.IsClass && t.Namespace == Data.App.ModelNamespace))
            {
                //Pass Type to XivDbAccess class
                try
                {
                    //If returned result is failure, log it and continue
                    if (!_access.CreateTable(model)) _log.Warning("Error occured during table creation for object [" + model.Name + "].");
                }
                catch (Exception exc)
                {
                    ExceptionHandler.HandleException(exc);
                    _log.Fatal("Fatal error occured during processing model classes...");
                }
            }
            //Finally, update dbstatus
            XivDbAccess.Status = DbStatus.Ok;
        }
        #endregion
    }
}