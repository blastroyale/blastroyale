using System;

namespace Quantum {
  public enum BTAbort {
    None,
    Self,
    LowerPriority,
    Both
  }

  public static class BTAbortExtensions {
    public static Boolean IsSelf(this BTAbort abort) {
      return abort == BTAbort.Self || abort == BTAbort.Both;
    }

    public static Boolean IsLowerPriority(this BTAbort abort) {
      return abort == BTAbort.LowerPriority || abort == BTAbort.Both;
    }
  }
}
