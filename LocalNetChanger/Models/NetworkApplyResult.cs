namespace LocalNetChanger.Models;

public readonly record struct NetworkApplyResult(bool Success, string Message, bool AlreadyActive = false);
