namespace Tasketier.App;

/// <summary>The help shown by the "?" buttons: the inline syntax and the keys.</summary>
public static class HelpTexts
{
    /// <summary>The inline syntax, one mark per line.</summary>
    public const string Syntax = """
        Words are the title. Marks can stand anywhere in the line:

        #tag            a tag (lower case, several allowed)
        !  or  !!       high or urgent priority; also as a suffix: Pay rent!!
        @today  @tomorrow        the due day
        @mon … @sun              the next such day after today
        @2026-11-03  @+3d  @+2w  a date, in 3 days, in 2 weeks
        @15:30          the due time; today when no day is given
        @none           no date (wins over other date marks)
        @alarm  @alarmN          an alarm 3 (or N) minutes before the due time (needs a time)

        Examples:
        Call the dentist @fri @15:30 ! #health
        Buy milk @tomorrow #home
        Renew passport @+2w !!
        """;

    /// <summary>The keys of the list window.</summary>
    public const string Shortcuts = """
        Enter in the top box     add the task
        Space / checkbox         complete or reopen the selected task
        Star                     plan for today without a date (carries over)
        Pencil / F2 / double-click   edit: the row becomes one line of marks; Enter saves, Esc cancels
        Del                      delete; Ctrl+Z brings it back
        Ctrl+Z                   undo the last change (20 kept per session)
        Ctrl+F                   the filter; Esc clears it
        Esc                      hide the window (it stays in the tray)

        An alarm shows an orange clock in the corner and beeps until you answer it:
        Done, Snooze 5 / 15 / 60 min, Disarm. The SNOOZE bar, Esc and Alt+F4 snooze 1 min. Only Disarm acknowledges.
        A snooze is stored with the task. An alarm more than one hour after its due time is not shown.
        The default lead (3 min) is in Settings.
        """;
}
