﻿## Closing tabs

tabs-close-button = Close
tabs-close-warning =
    You are about to close {$tabCount} tabs.
    Are you sure you want to continue?
tabs-close-tooltip = {$tabCount ->
    [0] No tabs
    [one] Close {$tabCount} tab
   *[other] Close {$tabCount} tabs
}


## Syncing

-sync-brand-name = Firefox Account

foo = Foo
    .attr = Foo Attribute
    .attr2 = Foo 2nd Attribute

ref-foo = { foo.attr }

sync-dialog-title = {-sync-brand-name}
sync-headline-title =
    {-sync-brand-name}: The best way to bring
    your data always with you
sync-signedout-title =
    Connect with your {-sync-brand-name}

## Datetime
date-is = The date is { DATETIME($dt, weekday: "short", month: "short", year: "numeric", day: "numeric", hour: "numeric", minute: "numeric", second: "numeric") }.
