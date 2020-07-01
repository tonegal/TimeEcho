// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using System.Data.SqlClient;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        private List<string> _dump_user_data(string username)
        {
            try
            {
                List<string> retList = new List<string>();
                retList.Add("MessageID | UserName | ScheduledTime | MessageText");
                retList.Add("--------------------------------------------------");

                StringBuilder sb = new StringBuilder();
                if (username != null) sb.Append("SELECT * FROM [dbo].[BotScheduledMessages] WHERE [UserName] = '" + username + "';");
                else sb.Append("SELECT * FROM [dbo].[BotScheduledMessages];");
                String sql = sb.ToString();

                using (SqlCommand command = new SqlCommand(sql, Program.botDBConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            retList.Add( reader.GetInt32(0) + " | " + reader.GetString(1) + " | " + 
                                            reader.GetDateTime(2).ToString() + " | " + reader.GetString(3));
                        }
                    }
                }

                return retList;
            }
            catch (SqlException e)
            {
                List<string> returnList = new List<string>();
                returnList.Add(e.ToString());
                return returnList;
            }

        }

        private int _add_user_data(string username, string message)
        {
            try
            {
                String insertSQL = @"INSERT INTO [dbo].[BotScheduledMessages](UserName, ScheduledTime, MessageText)
                     Values(@UserName, @ScheduledTime, @MessageText)";

                using (SqlCommand command = new SqlCommand(insertSQL, Program.botDBConnection))
                {
                    command.Parameters.AddWithValue("@UserName", username);
                    command.Parameters.AddWithValue("@ScheduledTime", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@MessageText", message);
                    return command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                return 0;
            }
        }

        private int _remove_user_data(string ID)
        {
            try
            {
                String deleteSQL = @"DELETE FROM [dbo].[BotScheduledMessages] WHERE MessageID = @MessageID";

                using (SqlCommand command = new SqlCommand(deleteSQL, Program.botDBConnection))
                {
                    command.Parameters.AddWithValue("@MessageID", ID);
                    return command.ExecuteNonQuery();
                }
            }
            catch (SqlException e)
            {
                return 0;
            }

        }

        private string[] arrange_command_params(string input_msg, int nr_of_params)
        {
            string cleaned_str = Regex.Replace(input_msg, "\\s+", " ");
            string[] ret = new string[nr_of_params];
            for (int i = 0; i < nr_of_params && cleaned_str.Trim().Length > 0; i++)
            {
                int split_ix = cleaned_str.IndexOf(" ");
                if (split_ix > 0 && i < nr_of_params - 1)
                {
                    ret[i] = cleaned_str.Substring(0, split_ix);
                    cleaned_str = cleaned_str.Substring(split_ix + 1);
                }
                else
                {
                    ret[i] = cleaned_str;
                    cleaned_str = "";
                }
            }
                
            return ret;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string inmsg = turnContext.Activity.Text;

            var replyText = $"No predefined action for the input: \"{inmsg}\"";
            if (inmsg.StartsWith("time"))
            {
                replyText = $"\n\nCurrent time is: {System.DateTime.UtcNow.ToString()}";
            }
            if (inmsg.StartsWith("dump"))
            {
                replyText = "";
                string[] cmd_parts = arrange_command_params(inmsg, 2);
                foreach (var item in _dump_user_data(cmd_parts[1]))
                {
                    replyText += $"\n\n{item}";
                }
            }
            if (inmsg.StartsWith("insert"))
            {
                string[] cmd_parts = arrange_command_params(inmsg, 3);
                int success = _add_user_data(cmd_parts[1], cmd_parts[2]);
                if (success > 0)
                {
                    replyText = $"Data entered into SQL database: UserName[{cmd_parts[1]}] | MessageText[{cmd_parts[2]}]";
                }
                else replyText = "Error during insertion into the database!";
            }
            if (inmsg.StartsWith("delete"))
            {
                string[] cmd_parts = arrange_command_params(inmsg, 2);
                int success = _remove_user_data(cmd_parts[1]);
                if (success > 0)
                {
                    replyText = $"Data deleted from SQL database: ID[{cmd_parts[1]}]";
                }
                else replyText = "Error during deletion from database!";
            }
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
