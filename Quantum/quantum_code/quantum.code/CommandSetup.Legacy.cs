using System;
using Photon.Deterministic;

namespace Quantum {
  [Obsolete("'CommandSetup.CreateCommands' is now deprecated. " +
            "Implement a partial declaration of 'DeterministicCommandSetup.AddCommandFactoriesUser' instead, as shown on 'CommandSetup.User.cs' available on the SDK package. " +
            "Command instances can be used as factories of their own type.")]
  public static class CommandSetup {
    public static DeterministicCommand[] CreateCommands(RuntimeConfig gameConfig, SimulationConfig simulationConfig) {
      return null;
    }
  }
}