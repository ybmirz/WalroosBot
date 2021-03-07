using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AquacraftBot.Services.BotServices
{
    public class ErrorHandlers // can be called as the Exceptions class
    {
        public static async Task Process(CommandErrorEventArgs e, EventId eventId)
        {
            //string ContactUs = $" | **Contact Us:** [Discord]({GlobalData.serverInv}) <{GlobalData.mailto}>";
            //will add dev credits and services to be able to contact dev
            switch (e.Exception)
            {
                case CommandNotFoundException:
                    await BotServices.SendEmbedAsync(e.Context, "Command Not Found", e.Exception.Message, ResponseType.Missing)
                        .ConfigureAwait(false);
                    break;
                case InvalidOperationException:
                    await BotServices.SendEmbedAsync(e.Context, "Invalid Operation Exception", e.Exception.Message + $"| Please contact developer through {GlobalData.prefixes[0]}contact dev", ResponseType.Warning)
                        .ConfigureAwait(false);
                    e.Context.Client.Logger.LogError(eventId, $"{e.Exception.StackTrace} FROM {e.Exception.Source}");
                    break;
                case ArgumentNullException:
                case ArgumentException:
                    await BotServices.SendEmbedAsync(e.Context,
                        "Argument Exception",
                        $"Invalid or Missing Arguments. `{GlobalData.prefixes[0]}help {e.Command?.QualifiedName}`",
                        ResponseType.Warning).ConfigureAwait(false);
                    break;
                case UnauthorizedException:
                    await BotServices.SendEmbedAsync(e.Context,
                        "Unauthorized Exception",
                        $"One of us does not have the required permissions. Please contact developer through {GlobalData.prefixes[0]}contact dev.",
                        ResponseType.Warning).ConfigureAwait(false);
                    e.Context.Client.Logger.LogWarning(eventId, $"{e.Exception.StackTrace}"); // for now to check
                    break;
                case ChecksFailedException cfe: //attribute check from the cmd failed
                    string title = "Check Failed Exception";
                    foreach (var check in cfe.FailedChecks)
                        switch (check)
                        {
                            case RequirePermissionsAttribute perms:
                                await BotServices.SendEmbedAsync(e.Context, title,
                                    $"One of us does not have the following required permissions: ({perms.Permissions.ToPermissionString()})",
                                    ResponseType.Error).ConfigureAwait(false);
                                break;
                            case RequireUserPermissionsAttribute perms:
                                await BotServices.SendEmbedAsync(e.Context, title,
                                    $"You do not have the following sufficient permissions: ({perms.Permissions.ToPermissionString()})",
                                    ResponseType.Error).ConfigureAwait(false);
                                break;
                            case RequireBotPermissionsAttribute perms:
                                await BotServices.SendEmbedAsync(e.Context, title,
                                    $"I do not have the following sufficient permissions: ({perms.Permissions.ToPermissionString()})",
                                    ResponseType.Error).ConfigureAwait(false);
                                break;
                            case RequireNsfwAttribute:
                                await BotServices.SendEmbedAsync(e.Context, title,
                                    $"This command is only bound to NSFW Channels",
                                    ResponseType.Error).ConfigureAwait(false);
                                break;
                            case CooldownAttribute:
                                await BotServices.SendEmbedAsync(e.Context, title, $"Calm down there mate! Please wait a few more seconds.", ResponseType.Warning)
                                    .ConfigureAwait(false);
                                break;
                            default:
                                await BotServices.SendEmbedAsync(e.Context, title,
                                    $"Unknown Check triggered. Please contact the developer through {GlobalData.prefixes[0]}contact dev",
                                    ResponseType.Error).ConfigureAwait(false);
                                break;
                        }
                    break;
                    // the rest of the exceptions handled to the log directly.
            }
        }
    }
}
