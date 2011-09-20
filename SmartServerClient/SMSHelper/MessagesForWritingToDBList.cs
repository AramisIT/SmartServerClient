using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Aramis.SMSHelperNamespace
    {
    [Serializable]
    public class MessagesForWritingToDBList
        {
        string LOCAL_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string MESSAGES_PATH = "MessagesForWritingToDB.dat";

        public List<Message> MessageList
            {
            get;
            set;
            }

        public MessagesForWritingToDBList()
            {
            MessageList = new List<Message>();

            BinaryFormatter binFormat = new BinaryFormatter();
            string filepath = LOCAL_PATH + "\\" + MESSAGES_PATH;
            if ( File.Exists(filepath) )
                {
                Stream fStream = File.OpenRead(filepath);
                if ( fStream.Length != 0 )
                    {
                    try
                        {
                        this.MessageList = ( ( MessagesForWritingToDBList ) binFormat.Deserialize(fStream) ).MessageList;
                        }
                    catch
                        {
                        }
                    }
                fStream.Close();
                }
            }

        public void Serialize()
            {
            if ( MessageList.Count > 0 )
                {
                bool serialized = false;
                while ( !serialized )
                    {
                    try
                        {
                        BinaryFormatter binFormat = new BinaryFormatter();
                        Stream fStream = new FileStream(LOCAL_PATH + "\\" + MESSAGES_PATH, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
                        binFormat.Serialize(fStream, this);
                        fStream.Close();
                        }
                    catch
                        {
                        }
                    }
                }
            else
                {
                string filepath = LOCAL_PATH + "\\" + MESSAGES_PATH;
                if ( File.Exists(filepath) )
                    {
                    File.Delete(filepath);
                    }
                }
            }

        }
    }
