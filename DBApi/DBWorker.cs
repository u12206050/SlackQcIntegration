using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;

namespace DB
{
    public class DBWorker
    {
        private const int cMaxNoteLength = 4000;
        private string dbFilePath;
        private SQLiteConnection connection;
        private string tableName;
        
        public DBWorker(string dbFilePath)
        {
            this.dbFilePath = dbFilePath;
            tableName = "notes";
        }

        public void Open()
        {
            if (!File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);
            }
            connection = new SQLiteConnection("Data Source=" + dbFilePath + ";" + "Version=3;");
            connection.Open();

            string sql = "create table if not exists " + tableName + " (id BIGINT PRIMARY KEY ASC, note VARCHAR(" + cMaxNoteLength + "))";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }

        public List<DBNotesEntry> SelectNotes()
        {
            List<DBNotesEntry> notesEntriesList = new List<DBNotesEntry>();
            if (connection != null)
            {
                string sql = "SELECT id, note FROM " + tableName;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBNotesEntry entry = new DBNotesEntry();
                    entry.id = reader["id"].ToString();
                    entry.note = reader["note"].ToString();
                    notesEntriesList.Add(entry);
                }
            }
            return notesEntriesList;
        }

        public int InsertNote(string note)
        {
            if (connection != null)
            {
                string noteWithEscaping = note.Replace("\"", "\"\"");
                if (noteWithEscaping.Length < cMaxNoteLength)
                {
                    long id = GetMaxNotesId() + 1;
                    string sql = "INSERT INTO " + tableName + " (id, note) VALUES (" + id + ", " + "\"" + noteWithEscaping + "\")";
                    SQLiteCommand command = new SQLiteCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
            }
            return -1;
        }

        public int DeleteNote(string id)
        {
            if (connection != null)
            {
                string sql = "DELETE FROM " + tableName + " WHERE id = \"" + id + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        public int DeleteAllNotes()
        {
            if (connection != null)
            {
                string sql = "DELETE FROM " + tableName;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private long GetMaxNotesId()
        {
            if (connection != null)
            {
                string sql = "SELECT (MAX(id)) from " + tableName;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                long maxId = 0;
                Int64.TryParse(command.ExecuteScalar().ToString(), out maxId);
                return maxId;
            }
            return -1;
        }
    }
}
