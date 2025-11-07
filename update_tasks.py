#!/usr/bin/env python3
import re
from pathlib import Path

# Read the task plan file
task_plan_path = Path(r'C:\Users\User\Desktop\SurveyBot\agents\out\task-plan.yaml')
content = task_plan_path.read_text()

# Tasks that should be marked as completed (Phase 1 and Phase 2)
completed_tasks = list(range(1, 31))  # TASK-001 to TASK-030

# For each completed task, update status to completed
for task_num in completed_tasks:
    task_id = f"TASK-{task_num:03d}"
    # Pattern to match task status: "pending" -> "completed"
    pattern = f'(- task_id: "{task_id}".*?)status: "pending"'
    replacement = r'\1status: "completed"\n      completed_date: "2025-11-05"'
    content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Write back
task_plan_path.write_text(content)
print(f"Updated {len(completed_tasks)} tasks to completed status")
