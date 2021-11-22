using UnityEngine;

namespace FirstLight.Editor.Build
{
    /// <summary>
    /// Run git commands.
    /// </summary>
    /// <remarks>
    /// Bits and pieces nicked from https://blog.somewhatabstract.com/2015/06/22/getting-information-about-your-git-repository-with-c/
    /// </remarks>
    public class GitProcess : ExternalProcess
    {
        private const string DefaultPathToGitBinary = "git";

        public GitProcess(string workingDir, string pathToGitBinary = DefaultPathToGitBinary)
            : base(workingDir, pathToGitBinary)
        {
        }

        /// <summary>
        /// Is this unity project a git repo?
        /// </summary>
        public bool IsValidRepo()
        {
            return ExecuteCommand("rev-parse --is-inside-work-tree") == "true";
        }

        /// <summary>
        /// Get the git branch name of the unity project.
        /// </summary>
        public string GetBranch()
        {
            return ExecuteCommand("rev-parse --abbrev-ref HEAD");
        }
        
        /// <summary>
        /// Get the git commit hash of the unity project.
        /// </summary>
        public string GetCommitHash()
        {
            return ExecuteCommand($"rev-parse HEAD");
        }
        
        /// <summary>
        /// Get the diff of the working directory in its current state from the state it was at at
        /// a given commit.
        /// </summary>
        public string GetDiffFromCommit(string commitHash)
        {
            return ExecuteCommand($"diff --word-diff=porcelain {commitHash} -- {Process.StartInfo.WorkingDirectory}");
        }
    }
}