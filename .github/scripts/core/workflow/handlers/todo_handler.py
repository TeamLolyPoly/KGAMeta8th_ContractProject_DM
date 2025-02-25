"""
TODO 처리를 담당하는 핸들러
"""
from typing import Dict, List, Tuple, Optional
from github.Repository import Repository
from github.Issue import Issue
from ..utils.logger import logger

class TodoProcessor:
    def __init__(self, repo: Repository, issue_number: Optional[int] = None):
        self.repo = repo
        self.issue_number = issue_number
        self.todos = []
        self.current_category = 'General'
    
    @staticmethod
    def is_issue_todo(todo_text: str) -> bool:
        """TODO 항목이 이슈 생성이 필요한지 확인합니다."""
        return todo_text.strip().startswith('(issue)')
    
    @staticmethod
    def convert_to_checkbox_list(text: str) -> str:
        """텍스트를 체크박스 목록으로 변환합니다."""
        if not text:
            return ''
        
        lines = []
        for line in text.strip().split('\n'):
            line = line.strip()
            if line:
                if line.startswith('@'):
                    lines.append(line)
                elif line.startswith(('-', '*')):
                    lines.append(f"- [ ] {line[1:].strip()}")
                else:
                    lines.append(f"- [ ] {line}")
        
        return '\n'.join(lines)
    
    @staticmethod
    def merge_todos(existing_todos: List[Tuple[bool, str]], new_todos: List[Tuple[bool, str]]) -> List[Tuple[bool, str]]:
        """기존 TODO와 새로운 TODO를 병합합니다."""
        todo_dict = {text: checked for checked, text in existing_todos}
        
        for checked, text in new_todos:
            if text not in todo_dict:
                todo_dict[text] = checked
        
        return [(todo_dict[text], text) for text in todo_dict]
    
    def process_todo_message(self, todo_text: str) -> List[Tuple[bool, str]]:
        """커밋 메시지의 TODO 섹션을 처리합니다."""
        if not todo_text:
            return []
        
        todo_lines = []
        for line in todo_text.strip().split('\n'):
            line = line.strip()
            if line:
                if line.startswith('@'):
                    self.current_category = line[1:].strip()
                    todo_lines.append((False, line))
                elif line.startswith(('-', '*')):
                    if '(issue)' in line:
                        text = line[1:].strip()  # '-' 제거
                        todo_lines.append((False, text))
                    else:
                        todo_lines.append((False, line[1:].strip()))
                else:
                    todo_lines.append((False, line))
        
        return todo_lines
    
    def process_existing_todos(self, existing_todos: List[Tuple[bool, str]]) -> List[Tuple[bool, str]]:
        """기존 TODO 항목들을 처리합니다."""
        if not existing_todos:
            return []
            
        processed = []
        for checked, text in existing_todos:
            if text.startswith('@'):
                self.current_category = text[1:].strip()
            processed.append((checked, text))
        return processed
    
    def create_issue_from_todo(self, todo_text: str) -> Optional[Issue]:
        """TODO 항목으로부터 새 이슈를 생성합니다."""
        if not self.is_issue_todo(todo_text):
            return None
            
        title = todo_text.replace('(issue)', '', 1).strip()
        issue_title = f"[{self.current_category}] {title}"
        
        try:
            new_issue = self.repo.create_issue(
                title=issue_title,
                body=self._create_issue_body(title),
                labels=['todo-generated', f'category:{self.current_category}']
            )
            
            if self.issue_number:
                parent_issue = self.repo.get_issue(self.issue_number)
                parent_issue.create_comment(f"Created issue #{new_issue.number} from todo item")
                
            return new_issue
        except Exception as e:
            logger.error(f"Failed to create issue for todo: {title}")
            logger.error(f"Error: {str(e)}")
            return None
    
    def _create_issue_body(self, title: str) -> str:
        """이슈 본문을 생성합니다."""
        return f"""## 📌 Task Description
{title}

## 🏷 Category
{self.current_category}

## 🔗 References
- Created from Daily Log: #{self.issue_number}"""

    def process_todos(self, commit_data: Optional[Dict] = None, existing_todos: Optional[List[Tuple[bool, str]]] = None, is_new_day: bool = False) -> Tuple[List[Tuple[bool, str]], List[Issue]]:
        """TODO 항목들을 처리합니다."""
        all_todos = []
        created_issues = []
        
        if existing_todos:
            if is_new_day:
                all_todos.extend([(checked, text) for checked, text in existing_todos 
                                if not checked or text.startswith('@')])
            else:
                all_todos.extend(self.process_existing_todos(existing_todos))
        
        if commit_data and commit_data.get('todo'):
            new_todos = self.process_todo_message(commit_data['todo'])
            all_todos = self.merge_todos(all_todos, new_todos)
        
        processed_todos = []
        for checked, text in all_todos:
            if text.startswith('@'):
                self.current_category = text[1:].strip()
                processed_todos.append((checked, text))
            elif self.is_issue_todo(text):
                new_issue = self.create_issue_from_todo(text)
                if new_issue:
                    created_issues.append(new_issue)
                    processed_todos.append((checked, f"#{new_issue.number}"))
            else:
                processed_todos.append((checked, text))
        
        return processed_todos, created_issues 