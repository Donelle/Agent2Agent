---
description: Reads all changes happened in the commit
---

Read all the changes happened in the current commit, specified commit, or staged changes and then summarize them.
- If a commit id is specified, it will be used to retrieve the commit information.
- If staged changes are present, they will be included in the summary.
- Only include commits after HEAD

Perform the following steps:

1. Create a title message representing the changes made in the commit.
2. Create a summary of the changes made in the commit in bullet points.
3. Include any relevant context or background information that helps to understand the changes.
4. If there are any breaking changes, highlight them and suggest migration paths if applicable.
5. Add the author information.
6. Add timestamp of the change in YYYY-MM-DD format.
7. Do not include any implementation details or code snippets.
8. Do not create markdown file.