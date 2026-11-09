# 滚回底部仍跟随，结果框不被切掉

结果区滚回最底下后，新输出停在最新一行。

看前面几行时，结果框被窗口下沿切到就整框移回窗口，正在看的行不变。

## 验收结果

本机中继 `127.0.0.1:17991`。四十行先停在 `queqiao-line-40` 和 `exit=0`。滚到开头再滚回末尾，再打出四十二行，眼前是 `queqiao-line-42` 和 `exit=0`，第一行不在。

结果框下沿伸出窗口时只露出前几行。点「提议一项改动」后，整框在窗口里，仍是 `queqiao-line-1` 到 `queqiao-line-8`。

图：`acceptance/desktop-refollow-before.png`、`desktop-refollow-after.png`、`desktop-fit-before.png`、`desktop-fit-after.png`。

`npm run check` 31 项通过。`cargo test` 23 项通过。发布构建后做了上述点击。不是两台电脑，也不是生产中继。剪贴板错误 5 仍在，本轮没有把它当成已修好。

## 下一轮只做这两件

1. 需求：点「之前的结果」能看到上一次输出，当前结果框仍留在窗口里。
2. 小修：结果框露全时，上面的「要做什么」不要被窗口上沿切掉。
