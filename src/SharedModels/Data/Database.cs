﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.DataAccess.Client;
using SharedModels.Debug;

namespace SharedModels.Data
{
    public class Database
    {
        private static readonly string _connectionString = Properties.Settings.Default.ConnectionString; // Oracle DB on InfraLab

        /// <summary>
        /// Executes the given query using the ExecuteReader method
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="args">List of OracleParameters containing the named arguments</param>
        /// <returns>A string[][] representing the result</returns>
        public static List<List<string>> ExecuteReader(string query, List<OracleParameter> args = null)
        {
            var result = new List<List<string>>();
            var con = new OracleConnection(_connectionString);

            try
            {
                using (var command = new OracleCommand(query, con) {BindByName = true})
                {
                    using (var connection = command.Connection)
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            try { connection.Open(); } catch (Exception e) { Logger.Write(e.Message); }
                        }

                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;

                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                command.Parameters.Add(arg);
                            }
                        }

                        var queryResult = command.ExecuteReader();
                        if (!queryResult.HasRows) { return result; }

                        while (queryResult.Read())
                        {
                            var record = new string[queryResult.FieldCount];
                            for (var i = 0; i < queryResult.FieldCount; i++)
                            {
                                var val = queryResult.GetValue(i);
                                if (DBNull.Value.Equals(val))
                                {
                                    var type = queryResult.GetFieldType(i);
                                    if (type == typeof(string) || type == typeof(DateTime))
                                    {
                                        record[i] = string.Empty;
                                    }
                                    else
                                    {
                                        record[i] = "0"; // Used for defaulting integers in the domain models
                                    }
                                }
                                else
                                {
                                    record[i] = val.ToString();
                                }
                            }
                            result.Add(record.ToList());
                        }
                    }
                }
            }
            catch (OracleException e)
            {
                Logger.Write(e.Message);
                return null;
            }
            finally
            {
                Close(con);
            }

            return result;
        }

        /// <summary>
        /// Executes the given query using the ExecuteReader method
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="args">List of OracleParameters containing the named arguments</param>
        /// <returns>A list of dictionaries representing the resulting records
        /// Key represents the column name
        /// Value represents the value of a specific column</returns>
        public static List<Dictionary<string, string>> ExecuteReaderDict(string query, List<OracleParameter> args = null)
        {
            var result = new List<Dictionary<string, string>>();
            var con = new OracleConnection(_connectionString);

            try
            {
                using (var command = new OracleCommand(query, con) {BindByName = true})
                {
                    using (var connection = command.Connection)
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            try { connection.Open(); } catch (OracleException e) { Logger.Write(e.Message); }
                        }

                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                command.Parameters.Add(arg);
                            }
                        }

                        var queryResult = command.ExecuteReader();

                        while (queryResult.Read())
                        {
                            var record = new Dictionary<string, string>();
                            for (var i = 0; i < queryResult.FieldCount; i++)
                            {
                                record[queryResult.GetName(i)] = queryResult.GetValue(i).ToString();
                            }

                            result.Add(record);
                        }
                    }
                }
            }
            catch (OracleException e)
            {
                Logger.Write(e.Message);
                return null;
            }
            finally
            {
                Close(con);
            }

            return result;
        }

        /// <summary>
        /// Executes a query to the database, expecting no returning result
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="args">List of OracleParameters containing the named arguments</param>
        /// <returns>True if the query succeeded (can still be 0 rows affected)</returns>
        public static bool ExecuteNonQuery(string query, List<OracleParameter> args = null)
        {
            var result = -1;
            var con = new OracleConnection(_connectionString);

            try
            {
                using (var command = new OracleCommand(query, con) { BindByName = true })
                {
                    using (var connection = command.Connection)
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            try { connection.Open(); } catch (Exception e) { Logger.Write(e.Message); }
                        }

                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;

                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                command.Parameters.Add(arg);
                            }
                        }

                        command.ExecuteNonQuery();
                        result = int.Parse((command.Parameters[0].Value.ToString()));
                    }
                }
            }
            catch (OracleException e)
            {
                Logger.Write(e.Message);
                return false;
            }
            finally
            {
                Close(con);
            }

            return result >= 0;
        }

        /// <summary>
        /// Executes a query on the database, expecting one value to be returned
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="returnValue">Out parameter used for storing an oracle return value</param>
        /// <param name="args">List of OracleParameters containing the named arguments</param>
        /// <returns>True if the query succeeded (can still be 0 rows affected)</returns>
        public static bool ExecuteNonQuery(string query, out string returnValue, List<OracleParameter> args = null)
        {
            var result = -1;

            OracleParameter returnParameter = null;
            returnValue = string.Empty;

            var con = new OracleConnection(_connectionString);

            try
            {
                using (var command = new OracleCommand(query, con) { BindByName = true })
                {
                    using (var connection = command.Connection)
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            try { connection.Open(); } catch (Exception e) { Logger.Write(e.Message); }
                        }

                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;

                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                if (arg.Direction == ParameterDirection.ReturnValue)
                                {
                                    returnParameter = arg;
                                }
                                command.Parameters.Add(arg);
                            }
                        }

                        result = command.ExecuteNonQuery();

                        if (returnParameter != null)
                        {
                            returnValue = (returnParameter.Value.ToString());
                        }
                    }
                }
            }
            catch(OracleException e)
            {
                Logger.Write(e.Message);
                return false;
            }
            finally
            {
                Close(con);
            }

            return result >= 0;
        }

        /// <summary>
        /// Executes a query to the database, expecting one value to be returned
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="args">List of OracleParameters containing the named arguments</param>
        /// <returns>Object representing the result of the query</returns>
        public static object ExecuteScalar(string query, List<OracleParameter> args = null)
        {
            object result;
            var con = new OracleConnection(_connectionString);

            try
            {
                using (var command = new OracleCommand(query, con) { BindByName = true })
                {
                    using (var connection = command.Connection)
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            try { connection.Open(); } catch (Exception e) { Logger.Write(e.Message); }
                        }

                        command.Connection = connection;

                        if (args != null)
                        {
                            foreach (var arg in args)
                            {
                                command.Parameters.Add(arg);
                            }
                        }

                        result = command.ExecuteScalar();
                    }
                }
            }
            catch (OracleException e)
            {
                Logger.Write(e.Message);
                return null;
            }
            finally
            {
                Close(con);
            }

            return result;
        }

        private static void Close(OracleConnection connection)
        {
            try
            {
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                Logger.Write(e.Message);
            }
        }
    }
}
