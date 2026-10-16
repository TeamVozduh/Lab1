namespace ClassLibrary.Wp;

// One calculation returns the normalized goal, wp, explanation, steps, and triad.
public sealed record WpResult(
    string Post,
    string PostHumanReadable,
    string WeakestPrecondition,
    string WeakestPreconditionHumanReadable,
    string Trace,
    string Triad);

public static class WpEngine
{
    // Parse both inputs, include any domain requirements, then work backward
    // through the program to find the weakest starting condition.
    public static WpResult Calculate(string fragment, string postcondition)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        ArgumentNullException.ThrowIfNull(postcondition);

        var program = new WpParser(fragment).ParseProgram();
        var parsedPost = new WpParser(postcondition).ParsePostcondition();
        var post = WpLogic.Simplify(parsedPost);
        var trace = new TraceWriter();
        trace.Add("Цель (Post): " + WpLogic.Format(post));

        // A partial expression in Post must be defined in the final state too.
        var postDefined = WpLogic.Defined(parsedPost);
        var target = WpLogic.And(postDefined, post);
        if (postDefined is not BooleanConstant { Value: true })
            trace.Add("Определённость Post: " + WpLogic.Format(postDefined));

        var pre = PullBack(program, target, trace, string.Empty);
        var preText = WpLogic.Format(pre);
        var postText = WpLogic.Format(post);
        trace.Add("Итоговое wp: " + preText);

        return new WpResult(
            postText,
            "После выполнения: " + WpLogic.Describe(post),
            preText,
            "До выполнения требуется: " + WpLogic.Describe(pre),
            trace.ToString(),
            "{ " + preText + " } " + fragment.Trim() + " { " + postText + " }" +
            Environment.NewLine + "Если до выполнения " + WpLogic.Describe(pre) +
            ", то после выполнения " + WpLogic.Describe(post) + ".");
    }

    // The UI can explain Post while the user edits it, before a full calculation.
    public static string DescribePostcondition(string postcondition)
    {
        ArgumentNullException.ThrowIfNull(postcondition);
        var parsed = new WpParser(postcondition).ParsePostcondition();
        return "После выполнения: " + WpLogic.Describe(WpLogic.Simplify(parsed));
    }

    // wp is calculated from the last statement toward the first. The current
    // target is the condition required immediately after each statement.
    private static BooleanExpression PullBack(IReadOnlyList<Statement> statements,
                                              BooleanExpression target, TraceWriter trace,
                                              string indentation)
    {
        var current = target;
        for (var i = statements.Count - 1; i >= 0; i--)
        {
            switch (statements[i])
            {
                case AssignmentStatement assignment:
                {
                    // An assignment must be defined; replace its target variable
                    // with the assigned expression in the later condition.
                    var defined = WpLogic.Defined(assignment.Value);
                    var substituted = WpLogic.Substitute(current, assignment.Variable, assignment.Value);
                    current = WpLogic.And(defined, substituted);
                    trace.Add(indentation + "Через " + assignment.Variable + " := " +
                              WpLogic.Format(assignment.Value) + " → " + WpLogic.Format(current));
                    if (defined is not BooleanConstant { Value: true })
                        trace.Add(indentation + "  Условие определённости: " + WpLogic.Format(defined));
                    break;
                }

                case IfStatement branch:
                {
                    // Compute both branches against the same target, then select
                    // each result under its guard and require a valid guard.
                    var guard = WpLogic.Simplify(branch.Condition);
                    trace.Add(indentation + "Через if (" + WpLogic.Format(guard) +
                              "): считаем обе ветки от цели " + WpLogic.Format(current));
                    trace.Add(indentation + "  Ветка if:");
                    var thenPre = PullBack(branch.ThenBranch, current, trace, indentation + "    ");
                    if (branch.ThenBranch.Count == 0)
                        trace.Add(indentation + "    Пустая ветка → " + WpLogic.Format(thenPre));
                    trace.Add(indentation + "  Ветка else:");
                    var elsePre = PullBack(branch.ElseBranch, current, trace, indentation + "    ");
                    if (branch.ElseBranch.Count == 0)
                        trace.Add(indentation + "    Пустая ветка → " + WpLogic.Format(elsePre));

                    var guardedBranches = WpLogic.Or(
                        WpLogic.And(guard, thenPre), WpLogic.And(WpLogic.Not(guard), elsePre));
                    var guardDefined = WpLogic.Defined(branch.Condition);
                    current = WpLogic.And(guardDefined, guardedBranches);
                    trace.Add(indentation + "После if: " + WpLogic.Format(current));
                    if (guardDefined is not BooleanConstant { Value: true })
                        trace.Add(indentation + "  Условие определённости проверки: " +
                                  WpLogic.Format(guardDefined));
                    break;
                }

                default:
                    throw new InvalidOperationException("Неизвестный оператор программы");
            }
        }
        return current;
    }

    // Number the steps in the order they are produced by backward traversal.
    private sealed class TraceWriter
    {
        private readonly List<string> steps = new();

        public void Add(string text) => steps.Add($"{steps.Count + 1}. {text}");

        public override string ToString() => string.Join(Environment.NewLine, steps);
    }
}
