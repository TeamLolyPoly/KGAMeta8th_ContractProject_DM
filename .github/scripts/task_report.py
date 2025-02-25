import os
from pathlib import Path
from github import Github
from datetime import datetime
import logging
import requests
from typing import Dict, Optional, Any, List, Set, Tuple
from dataclasses import dataclass
from enum import Enum
import re

# 로깅 설정 수정
logging.basicConfig(
    level=logging.DEBUG,  # INFO -> DEBUG
    format='%(asctime)s [%(levelname)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)
logger = logging.getLogger(__name__)

# 외부 라이브러리 로깅 레벨 조정
logging.getLogger('urllib3').setLevel(logging.WARNING)
logging.getLogger('github').setLevel(logging.WARNING)

# 1. 데이터 모델
@dataclass
class TodoInfo:
    title: str
    number: int
    status: str
    weight: int
    assignees: Set[str]
    closed_at: Optional[str]

@dataclass
class TaskInfo:
    number: int
    title: str
    status: 'TaskStatus'
    assignees: Set[str]
    priority: str
    expected_time: str
    todos: List[TodoInfo]
    category: str
    url: str

@dataclass
class TaskStatus:
    state: 'TaskState'
    progress: float

# 2. 상수 및 설정
class TaskState(Enum):
    WAITING = "⬜ 대기중"
    IN_PROGRESS = "🟡 진행중"
    COMPLETED = "✅ 완료"

    @property
    def icon(self) -> str:
        return self.value

    @property
    def order(self) -> int:
        return {
            TaskState.WAITING: 0,
            TaskState.IN_PROGRESS: 1,
            TaskState.COMPLETED: 2
        }[self]

class ReportSection(Enum):
    TASK_DETAILS = "## 📋 태스크 상세 내역"
    PROGRESS = "## 📊 진행 현황 요약"
    HISTORY = "## 📅 태스크 완료 히스토리"
    RISKS = "## 📝 특이사항 및 리스크"

    @property
    def marker(self) -> str:
        return self.value

    @property
    def order(self) -> int:
        return list(ReportSection).index(self)

# 3. 설정 데이터
TASK_CATEGORIES = {
    "기능 개발": {"emoji": "🔧", "name": "기능 개발", "description": "주요 기능 개발 태스크"},
    "UI/UX": {"emoji": "🎨", "name": "UI/UX", "description": "UI/UX 디자인 및 개선"},
    "QA/테스트": {"emoji": "🔍", "name": "QA/테스트", "description": "품질 보증 및 테스트"},
    "문서화": {"emoji": "📚", "name": "문서화", "description": "문서 작성 및 관리"},
    "유지보수": {"emoji": "🛠️", "name": "유지보수", "description": "버그 수정 및 유지보수"}
}

GITHUB_USER_MAPPING = {
    "Anxi77": {"name": "최현성", "role": "개발팀 팀장"},
    "beooom": {"name": "김범희", "role": "백엔드/컨텐츠 개발"},
    "Jine99": {"name": "김진", "role": "컨텐츠 개발"},
    "hyeonji9178": {"name": "김현지", "role": "컨텐츠 개발"},
    "Rjcode7387": {"name": "류지형", "role": "컨텐츠 개발"}
}

class GitHubProjectManager:
    def __init__(self, token: str, org: str = None, project_number: int = None):
        self.token = token
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github.v3+json"
        }
        self.g = Github(token)
        
        repo_name = os.environ.get('GITHUB_REPOSITORY', '')
        if '/' in repo_name:
            self.org = repo_name.split('/')[0]
        else:
            self.org = org or 'KGAMeta8thTeam1'
        
        logger.info(f"조직 설정: {self.org}")
        
        projects = self.list_projects()
        if projects:
            logger.info(f"사용 가능한 프로젝트 목록:")
            for p in projects:
                logger.info(f"  - #{p['number']}: {p['title']}")
            
            # 프로젝트 번호 설정
            if project_number and any(p['number'] == project_number for p in projects):
                self.project_number = project_number
            else:
                self.project_number = projects[0]['number']
                logger.info(f"프로젝트 번호 자동 설정: #{self.project_number}")
        else:
            # 프로젝트가 없으면 기본값 사용
            self.project_number = project_number or int(os.environ.get('PROJECT_NUMBER', '1'))
            logger.warning(f"프로젝트 목록을 가져올 수 없어 기본값 사용: #{self.project_number}")
        
        logger.info(f"GitHubProjectManager initialized with org: {self.org}, project: {self.project_number}")
        
    def _execute_graphql(self, query: str, variables: Dict[str, Any]) -> Optional[Dict]:
        """GraphQL 쿼리를 실행합니다."""
        try:
            response = requests.post(
                'https://api.github.com/graphql',
                json={'query': query, 'variables': variables},
                headers=self.headers
            )
            response.raise_for_status()
            result = response.json()
            
            if 'errors' in result:
                logger.error(f"GraphQL 오류: {result['errors']}")
                return None
            
            return result['data']
        except Exception as e:
            logger.error(f"GraphQL 쿼리 실행 중 오류 발생: {str(e)}")
            return None
    
    def get_project_info(self) -> Optional[Dict]:
        """프로젝트 정보와 필드 설정을 가져옵니다."""
        query = """
        query($org: String!, $number: Int!) {
            organization(login: $org) {
                projectV2(number: $number) {
                    id
                    title
                    url
                    fields(first: 20) {
                        nodes {
                            ... on ProjectV2SingleSelectField {
                                id
                                name
                                options {
                                    id
                                    name
                                }
                            }
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.org,
            "number": self.project_number
        }
        
        result = self._execute_graphql(query, variables)
        return result['organization']['projectV2'] if result else None
    
    def get_project_items(self) -> Dict:
        logger.info("프로젝트 아이템 조회 시작")
        
        query = """
        query($org: String!, $number: Int!) {
            organization(login: $org) {
                projectV2(number: $number) {
                    items(first: 100) {
                        nodes {
                            id
                            fieldValues(first: 100) {
                                nodes {
                                    ... on ProjectV2ItemFieldSingleSelectValue {
                                        name
                                        field {
                                            ... on ProjectV2SingleSelectField {
                                                name
                                            }
                                        }
                                    }
                                    ... on ProjectV2ItemFieldDateValue {
                                        date
                                        field {
                                            ... on ProjectV2Field {
                                                name
                                            }
                                        }
                                    }
                                    ... on ProjectV2ItemFieldNumberValue {
                                        number
                                        field {
                                            ... on ProjectV2Field {
                                                name
                                }
                            }
                        }
                    }
                }
                            content {
                                ... on Issue {
                                    number
                                    title
                                    url
                                    state
                                    createdAt
                                    closedAt
                                    labels(first: 100) {
                                        nodes {
                                            name
                                        }
                                    }
                                    assignees(first: 100) {
                        nodes {
                                            login
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        """
        
        result = self._execute_graphql(query, {
            "org": self.org,
            "number": self.project_number
        })
        
        if not result:
            logger.error("프로젝트 아이템을 가져오는데 실패했습니다.")
            return {}
        
        logger.debug("GraphQL 응답 데이터:")
        logger.debug(f"  - 조직: {self.org}")
        logger.debug(f"  - 프로젝트 번호: {self.project_number}")
        
        items = self._process_project_items(result)
        logger.info(f"총 {len(items)}개의 아이템을 가져왔습니다.")
        
        # 각 아이템의 상세 정보 로깅
        for number, item in items.items():
            logger.debug(f"\n아이템 #{number}:")
            logger.debug(f"  - 제목: {item['title']}")
            logger.debug(f"  - 상태: {item['state']}")
            logger.debug(f"  - 라벨: {', '.join(item['labels'])}")
            logger.debug(f"  - 담당자: {[a['login'] for a in item['assignees']]}")
            logger.debug(f"  - 필드값:")
            for field_name, field_value in item['fields'].items():
                logger.debug(f"    - {field_name}: {field_value}")
        
        return items
    
    def list_projects(self) -> list:
        """조직의 프로젝트 목록을 가져옵니다."""
        query = """
        query($org: String!) {
            organization(login: $org) {
                projectsV2(first: 10, orderBy: {field: CREATED_AT, direction: DESC}) {
                    nodes {
                        id
                        number
                        title
                    }
                }
            }
        }
        """
        
        result = self._execute_graphql(query, {"org": self.org})
        if not result or 'organization' not in result:
            logger.error(f"프로젝트 목록 조회 실패: {result}")
            return []
            
        return result['organization']['projectsV2']['nodes']

    def _process_project_items(self, result: Dict) -> Dict[int, Dict]:
        """GraphQL 결과를 처리하여 아이템 정보를 구성합니다."""
        logger.debug("\nGraphQL 응답 처리 시작")
        logger.debug(f"응답 데이터: {result}")
        
        items = {}
        for node in result['organization']['projectV2']['items']['nodes']:
            if not node['content']:
                logger.debug("컨텐츠가 없는 노드 발견, 건너뜀")
                continue
            
            issue = node['content']
            logger.debug(f"\n이슈 처리 시작: #{issue['number']}")
            logger.debug(f"원본 데이터: {issue}")
            
            item_data = {
                'number': issue['number'],
                'title': issue['title'],
                'url': issue['url'],
                'state': issue['state'],
                'created_at': issue['createdAt'],
                'closed_at': issue['closedAt'],
                'labels': [label['name'] for label in issue['labels']['nodes']],
                'assignees': [
                    {
                        'login': assignee['login']
                    }
                    for assignee in issue['assignees']['nodes']
                ],
                'fields': {}
            }
            
            logger.debug(f"변환된 데이터:")
            logger.debug(f"  - 번호: {item_data['number']}")
            logger.debug(f"  - 제목: {item_data['title']}")
            logger.debug(f"  - 상태: {item_data['state']}")
            logger.debug(f"  - 라벨: {item_data['labels']}")
            logger.debug(f"  - 담당자: {[a['login'] for a in item_data['assignees']]}")
            
            # 필드 값 처리
            for field_value in node['fieldValues']['nodes']:
                if not field_value or 'field' not in field_value:
                    continue
                
                field_name = field_value['field']['name']
                logger.debug(f"  - 필드 처리: {field_name}")
                
                if 'name' in field_value:  # SingleSelectValue
                    item_data['fields'][field_name] = field_value['name']
                    logger.debug(f"    - 선택값: {field_value['name']}")
                elif 'date' in field_value:  # DateValue
                    item_data['fields'][field_name] = field_value['date']
                    logger.debug(f"    - 날짜값: {field_value['date']}")
                elif 'number' in field_value:  # NumberValue
                    item_data['fields'][field_name] = field_value['number']
                    logger.debug(f"    - 숫자값: {field_value['number']}")
            
            items[issue['number']] = item_data
            logger.debug(f"아이템 #{issue['number']} 처리 완료")
        
        logger.debug(f"\n총 {len(items)}개의 아이템 처리 완료")
        return items

class TaskManager:
    """태스크와 투두 아이템의 상태 및 관계 관리"""
    def __init__(self, project_items: Dict, github_token: str):
        self.project_items = project_items
        self.g = Github(github_token)
        self.task_mapping = self._build_task_mapping()
        self.category_mapping = self._build_category_mapping()

    def _build_task_mapping(self) -> Dict[str, Dict]:
        """상위 태스크와 하위 투두 아이템 매핑을 구축"""
        logger.info("\n태스크 매핑 구축 시작")
        
        mapping = {}
        task_count = 0
        todo_count = 0
        
        repo = self.g.get_repo(os.environ.get('GITHUB_REPOSITORY'))
        
        # 프로젝트 아이템들에서 [태스크명]을 추출하여 매핑
        for item_data in self.project_items.values():
            title = item_data['title']
            match = re.match(r'\[(.*?)\]', title)
            if match:
                task_name = match.group(1)
                todo_count += 1
                
                # 해당 태스크가 없으면 생성
                if task_name not in mapping:
                    task_count += 1
                    # 실제 태스크 이슈 찾기
                    task_issue = None
                    for issue in repo.get_issues(state='all'):
                        if issue.title == f'[KGAMeta8th_ContractProject_DM] {task_name}':
                            task_issue = issue
                            break
                    
                    if task_issue:
                        # 태스크 제안서에서 예상 시간 추출
                        expected_time = None
                        if '구현목표일:' in task_issue.body:
                            try:
                                start_date = datetime.strptime('2025-02-21', '%Y-%m-%d')
                                end_date = datetime.strptime(task_issue.body.split('구현목표일:')[1].split('\n')[0].strip(), '%Y-%m-%d')
                                days = (end_date - start_date).days
                                expected_time = f"{days}d"
                            except:
                                expected_time = '-'
                        
                        mapping[task_name] = {
                            'number': task_issue.number,
                            'title': task_name,
                            'todos': [],
                            'assignees': set(),
                            'state': task_issue.state,
                            'completed_todos': 0,
                            'total_todos': 0,
                            'expected_time': expected_time or '-',
                            'url': task_issue.html_url
                        }
                    else:
                        mapping[task_name] = {
                            'number': task_count,
                            'title': task_name,
                            'todos': [],
                            'assignees': set(),
                            'state': 'OPEN',
                            'completed_todos': 0,
                            'total_todos': 0,
                            'expected_time': '-'
                        }
                
                # 투두 정보 추가
                todo_info = TodoInfo(
                    title=title.replace(f'[{task_name}] ', ''),
                    number=item_data['number'],
                    status='Done' if item_data['state'] == 'CLOSED' else 'In Progress',
                    weight=1,
                    assignees=set(a['login'] for a in item_data['assignees']),
                    closed_at=item_data['closed_at']
                )
                
                mapping[task_name]['todos'].append(todo_info)
                mapping[task_name]['assignees'].update(todo_info.assignees)
                mapping[task_name]['total_todos'] += 1
                if todo_info.status == 'Done':
                    mapping[task_name]['completed_todos'] += 1
        
        logger.info(f"\n태스크 매핑 통계:")
        logger.info(f"  - 총 태스크 수: {task_count}")
        logger.info(f"  - 총 투두 수: {todo_count}")
        
        return mapping

    def _build_category_mapping(self) -> Dict[str, List[TaskInfo]]:
        """카테고리별 태스크 매핑을 구축"""
        # 모든 태스크는 "기능 개발" 카테고리로
        mapping = {
            "기능 개발": []
        }
        
        repo_name = os.environ.get('GITHUB_REPOSITORY')
        
        for task_name, task_data in self.task_mapping.items():
            task_info = TaskInfo(
                number=task_data['number'] or 0,  # 번호가 없으면 0
                title=task_name,
                status=self.get_task_status(task_name),
                assignees=task_data['assignees'],
                priority="보통",  # 기본값
                expected_time=task_data['expected_time'],
                todos=task_data['todos'],
                category="기능 개발",
                url=f"https://github.com/{repo_name}/issues/{task_data['number']}"  # 이슈 URL 생성
            )
            mapping["기능 개발"].append(task_info)
        
        return mapping

    def get_task_status(self, task_name: str) -> TaskStatus:
        """태스크의 상태를 계산합니다."""
        task_data = self.task_mapping.get(task_name)
        if not task_data:
            return TaskStatus(TaskState.WAITING, 0.0)
        
        total = task_data['total_todos']
        completed = task_data['completed_todos']
        
        if total == 0:
            return TaskStatus(TaskState.WAITING, 0.0)
        
        progress = (completed / total) * 100
        
        if progress == 100:
            return TaskStatus(TaskState.COMPLETED, progress)
        elif progress > 0:
            return TaskStatus(TaskState.IN_PROGRESS, progress)
        return TaskStatus(TaskState.WAITING, 0.0)

    def get_tasks_by_category(self, category: str) -> List[TaskInfo]:
        """카테고리별 태스크 목록 반환"""
        return self.category_mapping.get(category, [])

    def get_all_completed_todos(self) -> List[Tuple[datetime, TodoInfo]]:
        """완료된 모든 투두 목록을 날짜순으로 반환"""
        completed_todos = []
        for item_data in self.project_items.values():
            # 투두 아이템인 경우만 처리
            if any(label.startswith('[TODO]') for label in item_data['labels']):
                if item_data['state'] == 'CLOSED' and item_data['closed_at']:
                    todo_info = self._create_todo_info(item_data)
                    completed_date = datetime.fromisoformat(item_data['closed_at'].replace('Z', '+00:00'))
                    completed_todos.append((completed_date, todo_info))
        
        return sorted(completed_todos, key=lambda x: x[0], reverse=True)

    def _create_todo_info(self, item_data: Dict) -> TodoInfo:
        """투두 정보 객체 생성"""
        return TodoInfo(
            title=item_data['title'].replace('[TODO] ', ''),
            number=item_data['number'],
            status='Done' if item_data['state'] == 'CLOSED' else 'In Progress',
            weight=self._get_item_weight(item_data),
            assignees=set(a['login'] for a in item_data['assignees']),
            closed_at=item_data['closed_at']
        )

    def _create_task_info(self, issue_number: int, item_data: Dict) -> TaskInfo:
        """태스크 정보 객체 생성"""
        title = item_data['title']
        status = self.get_task_status(title)
        return TaskInfo(
            number=issue_number,
            title=title,
            status=status,
            assignees=set(a['login'] for a in item_data['assignees']),
            priority=self._get_priority(item_data),
            expected_time=item_data['fields'].get('Target Date', '-'),
            todos=self.task_mapping.get(title, {}).get('todos', []),
            category=self._get_category(item_data),
            url=item_data['url']
        )

    def _get_item_weight(self, item_data: Dict) -> int:
        """아이템의 가중치를 반환합니다."""
        for label in item_data['labels']:
            if label.startswith('weight:'):
                try:
                    return int(label.replace('weight:', ''))
                except ValueError:
                    pass
        return 1

    def _get_priority(self, item_data: Dict) -> str:
        """아이템의 우선순위를 반환합니다."""
        for label in item_data['labels']:
            if label.startswith('priority:'):
                return label.replace('priority:', '').strip()
        return "보통"

    def _get_category(self, item_data: Dict) -> str:
        """아이템의 카테고리를 반환합니다."""
        for label in item_data['labels']:
            if label.startswith('category:'):
                category = label.replace('category:', '').strip()
                if category in TASK_CATEGORIES:
                    return category
        return "기타"

    def get_parent_task_title(self, todo_number: int) -> str:
        """투두 아이템의 상위 태스크 제목을 반환합니다."""
        todo_data = self.project_items.get(todo_number)
        if not todo_data:
            return "-"
        
        for label in todo_data['labels']:
            if label.startswith('[') and label.endswith(']') and not label.startswith('[TODO]'):
                return label[1:-1]
        return "-"

    def get_user_branch_url(self, username: str) -> str:
        """사용자의 개발 브랜치 URL을 생성합니다."""
        repo_name = os.environ.get('GITHUB_REPOSITORY')
        branch_name = f"Dev_{GITHUB_USER_MAPPING[username].get('branch_suffix', username.split('@')[0])}"
        return f"https://github.com/{repo_name}/tree/{branch_name}"

class ReportFormatter:
    def __init__(self, project_name: str, task_manager: TaskManager):
        self.project_name = project_name
        self.task_manager = task_manager
        self.current_date = datetime.now().strftime('%Y-%m-%d')

    def format_report(self) -> str:
        return f"""<div align="center">
{self._format_header()}
</div>

{self._format_basic_info()}
{self._format_team_info()}
{self._format_task_details()}
{self._format_progress_section()}
{self._format_task_history()}
{self._format_risks()}

---
> 이 보고서는 자동으로 생성되었으며, 담당자가 지속적으로 업데이트할 예정입니다.
"""

    def _format_header(self) -> str:
        """보고서 헤더를 생성합니다."""
        return """![header](https://capsule-render.vercel.app/api?type=transparent&color=39FF14&height=150&section=header&text=Project%20Report&fontSize=50&animation=fadeIn&fontColor=39FF14&desc=프로젝트%20진행%20보고서&descSize=25&descAlignY=75)

# 📊 프로젝트 진행보고서"""

    def _format_basic_info(self) -> str:
        """기본 정보 섹션을 생성합니다."""
        return f"""## 📌 기본 정보

**프로젝트명**: {self.project_name}  
**보고서 작성일**: {self.current_date}  
**보고 기간**: {self.current_date} ~ 진행중"""

    def _format_team_info(self) -> str:
        """팀원 정보 섹션을 생성합니다."""
        team_section = """## 👥 팀원 정보

| 깃허브 | 이름 | 역할 |
|--------|------|------|"""
        
        for username, info in GITHUB_USER_MAPPING.items():
            team_section += f"\n| @{username} | {info['name']} | {info['role']} |"
        
        return team_section

    def _format_task_details(self) -> str:
        """태스크 상세 내역을 포맷팅합니다."""
        details = """## 📋 태스크 상세 내역

<details>
<summary><h3>🔧 기능 개발</h3></summary>

| 태스크 ID | 태스크명 | 담당자 | 예상 시간 | 실제 시간 | 진행 상태 | 우선순위 |
| --------- | -------- | ------ | --------- | --------- | --------- | -------- |"""
        
        for task_name, task_data in sorted(self.task_manager.task_mapping.items()):
            status = self.task_manager.get_task_status(task_name)
            assignees_str = self._format_assignees(task_data['assignees'])
            
            details += f"\n| [TSK-{task_data['number']}] | {task_name} | {assignees_str} | - | - | {status.state.icon} ({status.progress:.1f}%) | - |"
        
        details += "\n</details>"
        return details

    def _format_tasks_table(self) -> str:
        """태스크 테이블을 포맷팅합니다."""
        tasks = self.task_manager.get_tasks_by_category("기능 개발")
        return "\n".join(self._format_task_entry(task) for task in tasks)

    def _format_task_entry(self, task: TaskInfo) -> str:
        """태스크 항목을 포맷팅합니다."""
        assignees_str = self._format_assignees(task.assignees)
        status_text = f"{task.status.state.icon} ({task.status.progress:.1f}%)"
        
        return f"| [TSK-{task.number}]({task.url}) | {task.title} | {assignees_str} | {task.expected_time} | - | {status_text} | {task.priority} |"

    def _format_todo_list(self, todos: List[TodoInfo]) -> str:
        """투두 목록을 포맷팅합니다."""
        if not todos:
            return ""
        
        result = "\n<details>\n<summary>📋 투두 목록</summary>\n\n"
        result += "| 투두 | 상태 | 가중치 | 담당자 |\n|------|--------|--------|--------|\n"
        
        for todo in todos:
            assignees_str = self._format_assignees(todo.assignees)
            result += f"| {todo.title} | {todo.status} | {todo.weight} | {assignees_str} |\n"
        
        return result + "\n</details>\n\n"

    def _format_progress_section(self) -> str:
        """진행 현황 섹션을 생성합니다."""
        return f"""## 📊 진행 현황 요약

### 전체 진행률

{self._format_overall_progress()}

### 📊 카테고리별 진행 현황

{self._format_category_progress()}

{self._format_daily_status()}"""

    def _format_overall_progress(self) -> str:
        """전체 진행률 섹션을 생성합니다."""
        stats = self._calculate_overall_stats()
        total = stats['total']
        completed = stats['completed']
        in_progress = stats['in_progress']

        progress = (completed / total * 100) if total > 0 else 0
        in_progress_rate = (in_progress / total * 100) if total > 0 else 0
        waiting_rate = ((total - completed - in_progress) / total * 100) if total > 0 else 100

        return f"""전체 진행 상태: {completed}/{total} 완료 ({progress:.1f}%)

```mermaid
pie title 전체 진행 현황
    "완료" : {progress:.1f}
    "진행중" : {in_progress_rate:.1f}
    "대기중" : {waiting_rate:.1f}
```"""

    def _format_category_progress(self) -> str:
        """카테고리별 진행 현황을 생성합니다."""
        # 태스크 매핑에서 직접 통계 계산
        stats = {}
        for task_name, task_data in self.task_manager.task_mapping.items():
            total = task_data['total_todos']
            completed = task_data['completed_todos']
            in_progress = total - completed
            
            if total > 0:
                progress_rate = (completed / total) * 100
            else:
                progress_rate = 0.0
            
            stats[task_name] = {
                'total': total,
                'completed': completed,
                'in_progress': in_progress,
                'progress_rate': progress_rate
            }
        
        progress = """| 태스크명 | 완료 | 진행중 | 대기중 | 진행률 |
| -------- | ---- | ------ | ------ | ------ |"""
        
        for task_name, stat in stats.items():
            progress += f"\n| {task_name} | {stat['completed']} | {stat['in_progress']} | {stat['total'] - stat['completed'] - stat['in_progress']} | {stat['progress_rate']:.1f}% |"
        
        return progress

    def _format_daily_status(self) -> str:
        """일자별 상세 현황을 생성합니다."""
        daily_stats = self._calculate_daily_stats()
        
        status = """### 📅 일자별 상세 현황

| 날짜 | 완료된 태스크 | 신규 태스크 | 진행중 태스크 |
| ---- | ------------- | ----------- | ------------- |"""
        
        for date, stats in sorted(daily_stats.items(), reverse=True):
            status += f"\n| {date} | {stats['completed']} | {stats['new']} | {stats['in_progress']} |"
        
        return status

    def _format_task_history(self) -> str:
        """태스크 완료 히스토리를 생성합니다."""
        history = "## 📅 태스크 완료 히스토리\n\n"
        
        # 완료된 투두 수집
        completed_todos = []
        for task_name, task_data in self.task_manager.task_mapping.items():
            for todo in task_data['todos']:
                if todo.status == 'Done' and todo.closed_at:
                    date = datetime.fromisoformat(todo.closed_at.replace('Z', '+00:00'))
                    completed_todos.append((date, todo, task_name))
        
        if not completed_todos:
            return history + "아직 완료된 태스크가 없습니다."
        
        # 날짜별로 정렬
        completed_todos.sort(key=lambda x: x[0], reverse=True)
        current_date = None
        
        for date, todo, task_name in completed_todos:
            date_str = date.strftime('%Y-%m-%d')
            if date_str != current_date:
                if current_date:
                    history += "</details>\n\n"
                count = sum(1 for d, _, _ in completed_todos if d.strftime('%Y-%m-%d') == date_str)
                history += f'<details>\n<summary><h3 style="display: inline;">📆 {date_str} ({count}개)</h3></summary>\n\n'
                history += "| 투두 ID | 투두명 | 상위 태스크 | 담당자 |\n|---------|--------|-------------|--------|\n"
                current_date = date_str
            
            assignees_str = self._format_assignees(todo.assignees)
            history += f"| #{todo.number} | {todo.title} | {task_name} | {assignees_str} |\n"
        
        if current_date:
            history += "</details>\n"
        
        return history

    def _format_assignees(self, assignees: Set[str]) -> str:
        """담당자 목록을 포맷팅합니다."""
        if not assignees:
            return "-"
        
        formatted = []
        for username in sorted(assignees):
            if username in GITHUB_USER_MAPPING:
                user_info = GITHUB_USER_MAPPING[username]
                branch_url = self.task_manager.get_user_branch_url(username)
                formatted.append(f"[{user_info['name']}]({branch_url})")
            else:
                formatted.append(f"@{username}")
        
        return ", ".join(formatted)

    def _calculate_overall_stats(self) -> Dict:
        """전체 통계를 계산합니다."""
        category_stats = {}  # 카테고리별 통계를 저장할 딕셔너리
        total_stats = {'total': 0, 'completed': 0, 'in_progress': 0}  # 전체 통계
        
        # 카테고리별 통계 계산
        for category in TASK_CATEGORIES:
            tasks = self.task_manager.get_tasks_by_category(category)
            completed = sum(1 for task in tasks if task.status.state == TaskState.COMPLETED)
            in_progress = sum(1 for task in tasks if task.status.state == TaskState.IN_PROGRESS)
            
            category_stats[category] = {
                'total': len(tasks),
                'completed': completed,
                'in_progress': in_progress
            }
            
            # 전체 통계에 더하기
            total_stats['total'] += len(tasks)
            total_stats['completed'] += completed
            total_stats['in_progress'] += in_progress
        
        return total_stats

    def _calculate_daily_stats(self) -> Dict:
        """일자별 통계를 계산합니다."""
        stats = {}
        today = datetime.now().strftime('%Y-%m-%d')
        stats[today] = {'completed': 0, 'new': 0, 'in_progress': 0}
        
        # 완료된 투두 카운트
        for task_data in self.task_manager.task_mapping.values():
            for todo in task_data['todos']:
                if todo.status == 'Done' and todo.closed_at:
                    date = datetime.fromisoformat(todo.closed_at.replace('Z', '+00:00')).strftime('%Y-%m-%d')
                    if date not in stats:
                        stats[date] = {'completed': 0, 'new': 0, 'in_progress': 0}
                    stats[date]['completed'] += 1
        
        # 진행중인 투두 카운트
        for task_data in self.task_manager.task_mapping.values():
            in_progress = sum(1 for todo in task_data['todos'] if todo.status == 'In Progress')
            if in_progress > 0:
                stats[today]['in_progress'] += in_progress
        
        return stats

    def _format_risks(self) -> str:
        """특이사항 및 리스크 섹션을 생성합니다."""
        return """## 📝 특이사항 및 리스크

| 구분 | 내용 | 대응 방안 |
| ---- | ---- | --------- |
| - | - | - |"""

class TaskReport:
    """태스크 보고서의 전체적인 상태와 동작을 관리"""
    
    def __init__(self, github_token: str):
        self.github_token = github_token
        self.github_manager = GitHubProjectManager(github_token)
        self.project_items = self.github_manager.get_project_items()
        self.task_manager = TaskManager(self.project_items, github_token)
        self.formatter = None

    def create_or_update_report(self, repo_name: str) -> None:
        """보고서를 생성하거나 업데이트합니다."""
        self.formatter = ReportFormatter(repo_name, self.task_manager)
        repo = self.github_manager.g.get_repo(os.environ.get('GITHUB_REPOSITORY'))
        
        report_issue = self._find_report_issue(repo, repo_name)
        if report_issue:
            self._update_report(report_issue)
            logger.info(f"기존 보고서 #{report_issue.number} 업데이트 완료")
        else:
            self._create_new_report(repo, repo_name)
            logger.info("새 보고서 생성 완료")

    def _find_report_issue(self, repo, repo_name: str) -> Optional[Any]:
        """보고서 이슈를 찾습니다."""
        report_title = f"[{repo_name}] 프로젝트 진행보고서"
        for issue in repo.get_issues(state='open'):
            if issue.title == report_title:
                return issue
        return None

    def _create_new_report(self, repo, repo_name: str) -> None:
        """새 보고서를 생성합니다."""
        report_body = self.formatter.format_report()
        repo.create_issue(
            title=f"[{repo_name}] 프로젝트 진행보고서",
            body=report_body,
            labels=['📊 진행중']
        )

    def _update_report(self, report_issue) -> None:
        """기존 보고서를 업데이트합니다."""
        current_body = report_issue.body
        updated_body = self._update_sections(current_body)
        report_issue.edit(body=updated_body)

    def _update_sections(self, current_body: str) -> str:
        """보고서의 각 섹션을 업데이트합니다."""
        logger.info("보고서 섹션 업데이트 시작")
        
        # 기본 정보와 팀원 정보는 유지
        basic_info_end = current_body.find("## 📋 태스크 상세 내역")
        header = current_body[:basic_info_end] if basic_info_end != -1 else ""
        
        # 각 섹션을 한 번만 추가
        sections = [
            self.formatter._format_task_details(),
            self.formatter._format_progress_section(),
            self.formatter._format_task_history(),
            self.formatter._format_risks()
        ]
        
        body = header.strip() + "\n\n" + "\n\n".join(sections)
        body += "\n\n---\n> 이 보고서는 자동으로 생성되었으며, 담당자가 지속적으로 업데이트할 예정입니다."
        
        logger.info("보고서 섹션 업데이트 완료")
        return body

def main():
    try:
        # 로그 레벨 설정
        logger.setLevel(logging.DEBUG)
        logger.info("프로그램 시작")
        
        # GitHub 토큰 확인
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        if not github_token:
            raise ValueError("GitHub 토큰이 설정되지 않았습니다.")
        logger.debug("GitHub 토큰 확인 완료")
        
        # 저장소 정보 확인
        repo_name = os.environ.get('GITHUB_REPOSITORY')
        if not repo_name:
            raise ValueError("GitHub 저장소 정보를 찾을 수 없습니다.")
        logger.debug(f"저장소 정보: {repo_name}")
        
        project_name = repo_name.split('/')[-1]
        logger.info(f"프로젝트 이름: {project_name}")
        
        # TaskReport 인스턴스 생성 및 보고서 관리
        logger.info("보고서 생성/업데이트 시작")
        report = TaskReport(github_token)
        
        # 데이터 상태 확인
        logger.debug(f"프로젝트 아이템 수: {len(report.project_items)}")
        logger.debug(f"태스크 매핑 수: {len(report.task_manager.task_mapping)}")
        logger.debug("카테고리별 태스크 수:")
        for category, tasks in report.task_manager.category_mapping.items():
            logger.debug(f"  - {category}: {len(tasks)}개")
        
        # 보고서 업데이트
        report.create_or_update_report(project_name)
        logger.info("보고서 생성/업데이트 완료")
        
    except Exception as e:
        logger.error(f"오류 발생: {str(e)}")
        logger.error(f"오류 상세: {type(e).__name__}")
        logger.error(f"스택 트레이스:", exc_info=True)
        raise

if __name__ == '__main__':
    main() 