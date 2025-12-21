---
agent: agent
---
# Git 智能提交助手

## 1. 检查分支
```bash
git branch --show-current
```
- 若在 `master`/`main`，根据变更内容创建新分支：`git checkout -b <type>/<简短描述>`
- 分支类型：`feature` | `fix` | `refactor` | `perf` | `docs` | `test` | `chore`

## 2. 分析变更
```bash
git diff --stat
git diff
```

## 3. 提交
```bash
git add .
git commit -m "<type>(<scope>): <描述>"
```

提交信息使用中文，格式：`feat(search): 添加模糊搜索功能`