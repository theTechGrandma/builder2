using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Mercurial;

namespace DSTBuilder.Models
{
    public class Mercurial
    {
        #region Mercurial Helpers

        public void CheckForMultipleHeads(string localRepo)
        {
            try
            {
                var repo = new Repository(localRepo);
                Changeset[] log = repo.Heads().ToArray();
                if (log.Length > 1)
                {
                    throw new Exception("DST build failure - Multiple heads detected. Build halted. Please contact someone who can fix it.");
                }
            }
            catch (MercurialExecutionException ex)
            {
                throw new MercurialExecutionException("DST build failure - Multiple heads detected. Build halted. Please contact someone who can fix it.", ex);
            }
        }

        public void Status(string fromVersion, string toVersion, string localRepo)
        {
            //method is currently not being used.
            try
            {
                var repo = new Repository(localRepo);
                var builder = new StringBuilder();

                string filename = localRepo + "\\Application\\Installation Scripts\\DOT\\IncomingOracleChanges.txt";
                using (var writeFile = new StreamWriter(filename, true))
                {
                    FileStatus[] status =
                        repo.Status(
                            new StatusCommand().WithAdditionalArgument("--rev " + fromVersion + ":" + toVersion +
                                                                       " --modified --added")).ToArray();
                    foreach (var changes in status)
                    {
                        if (changes.State.ToString() == "Removed") continue;
                        builder.Append(changes.Path);
                        builder.AppendLine();
                    }
                    writeFile.Write(builder);
                }
            }
            catch (MercurialExecutionException ex)
            {
                throw new MercurialExecutionException("Oracle diff failed for Mercurial.", ex);
            }
        }

        public string Log(string localRepo, string lastBuildVersion)
        {
                var repo = new Repository(localRepo);
                var builder = new StringBuilder();
                Changeset[] status = repo.Log(new LogCommand().WithAdditionalArgument("--rev " + lastBuildVersion + ":tip")).ToArray();
                foreach (var changes in status)
                {
                    builder.AppendLine();
                    builder.Append(changes.AuthorName);
                    builder.Append('-');
                    builder.Append(changes.CommitMessage);
                    builder.Append('-');
                    builder.Append(changes.Revision);
                    builder.AppendLine();
                }

                return builder.ToString();
        }

        public void Pull(string local, string remote)
        {
            try
            {
                var repo = new Repository(local);

                repo.Pull(remote, new PullCommand
                {
                    Update = true,
                });
            }
            catch (MercurialExecutionException ex)
            {
                throw new MercurialExecutionException("The source pull failed.", ex);
            }
        }

        public void PullByBranch(string local, string remote, string version)
        {
            try
            {
                var repo = new Repository(local);
                repo.Pull(remote, new PullCommand
                {
                    Branches =
                        {
                            version,
                        },
                    Update = true,
                });
            }
            catch (MercurialExecutionException ex)
            {
                throw new MercurialExecutionException("The Help pull failed.", ex);
            }

        }

        public void CommitAndPushTag(string localRepo, string remote, string message, string version)
        {
            try
            {
                var repo = new Repository(localRepo);
                repo.Commit(new CommitCommand().WithMessage(message).WithAddRemove(true));
                repo.Tag(version);

                repo.Push(remote, new PushCommand
                {
                    AllowCreatingNewBranch = false,
                    Force = false
                });
            }
            catch (MercurialExecutionException ex)
            {
                throw new MercurialExecutionException("The commit and push failed.", ex);
            }
            
            }

        #endregion

    }
}