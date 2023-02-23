/*
 * FILE:        MainWindow.xaml.cs
 * Project:	    A03 – SERVICES AND LOGGING
 * Author:	    Hoang Phuc Tran - ID: 8789102
                Bumsu Yi - ID: 8110678
 * Date:		February 21, 2022
 * Description: This file contains all event handlers for UI in XAML file.
 */
using System.Windows.Documents;
using System.Net.Sockets;
using System.Windows;
using Newtonsoft.Json;
using System;
using System.Windows.Controls;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace SENG2040_A3
{
    /// <summary>
    /// CLASS NAME:  MainWindow
    /// PURPOSE : The MainWindow class is inherited from the Window classes.It has properties
    /// and event handlers. This class is used to handle UI in XAML file.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variables is used for TCP connection.
        TcpClient client_TCP;
        StreamReader reading;
        StreamWriter writing;

        // classes is used to receive and send date back and forth.
        LoggingServiceMessage log_message = new LoggingServiceMessage("Log");
        LoggingServieLoginResponse check_response;


        /*  -- Method Header Comment
        Name	: MainWindow -- CONSTRUCTOR
        Purpose : to instantiate a new MainWindow object
        Inputs	: NONE
        Outputs	: NONE
        Returns	: NONE
        */
        public MainWindow()
        {
            InitializeComponent();

            // Set the UI in the "logged out" state
            UI_Update(false);
        }

        /*  -- Event handler Header Comment
          Name	: btnLogout_Click
          Purpose : this event handler is used to implenment "Exit" close the connection and disable the UI
          Inputs	: sender               object
                      RoutedEventArgs       e
          Outputs	: NONE
          Returns	: NONE
        */
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            // close the connection and 
            client_TCP.Close();

            // Disable the UI
            UI_Update(false);
        }

        /*  -- Event handler Header Comment
          Name	: btnLogin_Click
          Purpose : this event handler is used to handle the login and setup TCP connection.
          Inputs	: sender               object
                      RoutedEventArgs       e
          Outputs	: NONE
          Returns	: NONE
        */
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Create a connection for the IP address from the user's input
            TcpClient logging = new TcpClient();

            Int32.TryParse(loggingPort.Text, out int port);
            logging.Connect(IPAddress.Parse(loginIP.Text), port);

            // Set up the JSON login
            string loggingMessage = $"{{\"User\":\"{userName.Text}\", \"Key\":\"{loggingKey.Text}\" }}";

            if (logging.Connected == true)
            {
                // Create the writer and send the login JSON to request the login 
                writing = new StreamWriter(logging.GetStream());
                writing.Write(loggingMessage);
                writing.Flush();

                reading = new StreamReader(logging.GetStream());

                // Read the response from the server
                char[] _buffer = new char[1024];
                int count_byte = reading.Read(_buffer, 0, _buffer.Length);
                string logging_response = null;
                logging_response = new string(_buffer, 0, count_byte);

                // Deserialize the response and print it into the textbox
                check_response = JsonConvert.DeserializeObject<LoggingServieLoginResponse>(logging_response);
                reponse_text.Text = JsonConvert.SerializeObject(check_response, Formatting.Indented);

                // Give the session key to the places that need it
                log_message.Session_Key = check_response.Session_Key;
                log_message.User = check_response.User;
                loggingKey.Text = check_response.Key;

                // Create the new TCP for logging destination.
                client_TCP = new TcpClient();
                client_TCP.Connect(IPAddress.Parse(loginIP.Text), check_response.Port);

                reading = new StreamReader(client_TCP.GetStream());
                writing = new StreamWriter(client_TCP.GetStream());

                // Update the UI and enable user to use buttons
                UI_Update(true);

                // Update the UI if it has any change
            }
        }

        /*  -- Method Header Comment
          Name	: UI_Update
          Purpose : this function is used to disable and enalbe the UI.
          Inputs	: bool    enable
          Outputs	: NONE
          Returns	: NONE
        */
        void UI_Update(bool enable)
        {
            loggingPort.IsEnabled = !enable;
            userName.IsEnabled = !enable;
            btnLogout.IsEnabled = enable;
            loginIP.IsEnabled = !enable;
            btnLogin.IsEnabled = !enable;
            loggingKey.IsEnabled = !enable;
        }


        /*  -- Method Header Comment
          Name	: setting_command
          Purpose : this function is used to disable and enalbe the UI.
          Inputs	: string overideMessage (Default: null): string which if set overrides any JSON to be sent and simply sends this string in its place
          Outputs	: NONE
          Returns	: NONE
        */
        public void setting_command(string overideMessage = null)
        {

            // send the message of the preview has
            if (overideMessage == null)
            {
                writing.Write(new TextRange(rtbCommandPreview.Document.ContentStart,
                    rtbCommandPreview.Document.ContentEnd).Text);
            }
            // If the string is not null, we overide the message
            else
            {
                writing.Write(overideMessage);
            }

            writing.Flush();

            char[] _buffer = new char[1024];
            int count_byte = reading.Read(_buffer, 0, _buffer.Length);

            // Dynamically parse the returned json and display it in whatever form it is in to the response textbox
            string data_string = new string(_buffer, 0, count_byte);
            dynamic json = JsonConvert.DeserializeObject(data_string);
            reponse_text.Text = JsonConvert.SerializeObject(json, Formatting.Indented);
        }

        /*  -- Method Header Comment
          Name	: previewMessage
          Purpose : this function is used to display and preview the message.
          Inputs	: string     stringPreview
          Outputs	: NONE
          Returns	: NONE
        */
        private void previewMessage(string stringPreview)
        {
            rtbCommandPreview.Document.Blocks.Clear();
            rtbCommandPreview.Document.Blocks.Add(new Paragraph(new Run(stringPreview)));
        }


        /*  -- Method Header Comment
          Name	: previewMessage
          Purpose : this function is used to update the log message.
          Inputs	: NONE
          Outputs	: NONE
          Returns	: NONE
        */
        private void updating_message_log()
        {
            log_message.Parameters.FileName = tbFileName.Text;
            log_message.Parameters.Level = tbLogLevel.Text;
            log_message.Parameters.Message = tbMessage.Text;
            log_message.Parameters.Tags = tbTags.Text.Split(',');
            log_message.Parameters.Timestamp = DateTime.Now.ToString();
            log_message.Parameters.FileLine = Int32.TryParse(tbFileLine.Text, out int x) ? Int32.Parse(tbFileLine.Text) : 0;
        }

        /**
         * Function:    btnSendSequentialLogs_Click
         * Description: Sends a set number of sequential logs to the logging service, going from 1 -> X of FileLine 
         * Parameters:  STANDARD_EVENT_HANDLER
         * Returns:     void
         */

        /*  -- Event handler Header Comment
          Name	: btnLogin_Click
          Purpose : this event handler is used to handle the login and setup TCP connection.
          Inputs	: sender               object
                      RoutedEventArgs       e
          Outputs	: NONE
          Returns	: NONE
        */
        private void btnSendSequentialLogs_Click(object sender, RoutedEventArgs e)
        {
            Int32.TryParse(numberLog.Text, out int sequenceSize);

            if (sequenceSize > 0)
            {
                for (int i = 1; i <= sequenceSize; i++)
                {
                    tbFileLine.Text = i.ToString();
                    setting_command();
                }
                previewMessage($"Completed sending {sequenceSize} logs to the service!");
            }
            else
            {
                previewMessage("Invalid X value");
            }
        }



        /**
         * Function:    btnMalformedJson_Click
         * Description: Send a message with malformed JSON to the logging service
         * Parameters:  STANDARD_EVENT_HANDLER
         * Returns:     void
         */
        private void btnMalformedJson_Click(object sender, RoutedEventArgs e)
        {
            string malformedJSON = "{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{}";
            previewMessage(malformedJSON);
            setting_command(malformedJSON);
        }



        /**
         * Function:    btnSendIncorrectSession_Click
         * Description: Send a "Log" action with an incorrect session in the JSON
         * Parameters:  STANDARD_EVENT_HANDLER
         * Returns:     void
         */
        private void btnSendIncorrectSession_Click(object sender, RoutedEventArgs e)
        {
            string tmpSession = log_message.Session_Key;
            log_message.Session_Key = "INCORRECT SESSION";
            setting_command();
            log_message.Session_Key = tmpSession;
        }

    }
}
