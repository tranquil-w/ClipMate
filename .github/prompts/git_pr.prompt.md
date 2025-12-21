---
agent: agent
---
# PR 自动创建助手

当用户请求创建 PR 时，执行以下步骤：

## 1. 分析变更
- 运行 `git diff master...HEAD --stat` 查看变更文件
- 运行 `git log master..HEAD --oneline` 查看提交历史
- 根据变更内容确定 PR 类型

## 2. 生成 PR 标题
格式：`<类型>: <简短描述>`

类型：`feat` | `fix` | `perf` | `refactor` | `docs` | `test` | `chore`

## 3. 生成 PR 描述
```markdown
## 变更说明
<根据 diff 和 commit 总结主要变更>

## 变更类型
- [x] <勾选对应类型>

## 测试
- [ ] 单元测试已通过
- [ ] 手动测试已执行
```

## 4. 创建 PR
```bash
# 确保分支已推送
git push -u origin $(git branch --show-current)

# 创建 PR
gh pr create --title "<标题>" --body "<描述>"
```

## 5. 输出 PR 链接
创建成功后显示 PR URL 供用户查看。