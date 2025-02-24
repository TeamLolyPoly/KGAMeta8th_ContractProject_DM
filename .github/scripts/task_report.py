import os
import sys
from pathlib import Path
from github import Github
from datetime import datetime
import re
import json
import logging
import requests
from typing import Dict, Optional, Any

# 로깅 설정 수정
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)

# stdout 핸들러 추가
handler = logging.StreamHandler(sys.stdout)
handler.setLevel(logging.INFO)
formatter = logging.Formatter('%(asctime)s [%(levelname)s] %(message)s')
handler.setFormatter(formatter)
logger.addHandler(handler)

class GitHubProjectManager:
    def __init__(self, token: str, org: str = None, project_number: int = None):
        self.token = token
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github.v3+json"
        }
        self.g = Github(token)
        
        # GitHub 조직과 프로젝트 번호 설정
        repo_name = os.environ.get('GITHUB_REPOSITORY', '')
        if '/' in repo_name:
            self.org = repo_name.split('/')[0]
        else:
            self.org = org or 'KGAMeta8thTeam1'
        
        logger.info(f"조직 설정: {self.org}")
        
        # 사용 가능한 프로젝트 목록 조회
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
    
    def get_project_items(self) -> Dict[int, Dict]:
        """프로젝트의 모든 아이템 정보를 가져옵니다."""
        query = """
        query($org: String!, $number: Int!) {
            organization(login: $org) {
                projectV2(number: $number) {
                    items(first: 100) {
                        nodes {
                            id
                            content {
                                ... on Issue {
                                    id
                                    number
                                    title
                                    state
                                    createdAt
                                    closedAt
                                    labels(first: 10) {
                                        nodes {
                                            name
                                        }
                                    }
                                    assignees(first: 5) {
                                        nodes {
                                            login
                                        }
                                    }
                                }
                            }
                            fieldValues(first: 8) {
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
                                }
                            }
                        }
                    }
                }
            }
        }
        """
        
        result = self._execute_graphql(query, {"org": self.org, "number": self.project_number})
        if not result:
            return {}
            
        items = result['organization']['projectV2']['items']['nodes']
        project_items = {}
        
        for item in items:
            if not item['content']:
                continue
                
            issue = item['content']
            issue_number = issue['number']
            
            # 기본 이슈 정보
            item_data = {
                'number': issue['number'],
                'title': issue['title'],
                'state': issue['state'],
                'created_at': issue['createdAt'],
                'closed_at': issue['closedAt'],
                'labels': [label['name'] for label in issue['labels']['nodes']],
                'assignees': [assignee['login'] for assignee in issue['assignees']['nodes']],
                'fields': {}
            }
            
            # 프로젝트 필드 값 처리
            for value in item['fieldValues']['nodes']:
                if not value:
                    continue
                    
                field_name = value['field']['name']
                if 'date' in value:
                    item_data['fields'][field_name] = value['date']
                else:
                    item_data['fields'][field_name] = value['name']
            
            # 카테고리 결정
            category = "기타"
            for label in item_data['labels']:
                if label.startswith('category:'):
                    category = label.replace('category:', '').strip()
                    break
            item_data['category'] = category
            
            # 진행 상태 결정
            status = item_data['fields'].get('Status', 'Todo')
            item_data['status'] = status
            
            # 시작일/종료일 처리
            item_data['start_date'] = item_data['fields'].get('Start Date')
            item_data['target_date'] = item_data['fields'].get('Target Date')
            
            project_items[issue_number] = item_data
        
        return project_items
    
    def add_issue_to_project(self, issue_node_id: str) -> Optional[str]:
        """이슈를 프로젝트에 추가합니다."""
        query = """
        mutation($project: ID!, $issue: ID!) {
            addProjectV2ItemById(input: {projectId: $project, contentId: $issue}) {
                item {
                    id
                }
            }
        }
        """
        
        project_info = self.get_project_info()
        if not project_info:
            return None
            
        variables = {
            "project": project_info['id'],
            "issue": issue_node_id
        }
        
        result = self._execute_graphql(query, variables)
        return result['addProjectV2ItemById']['item']['id'] if result else None
    
    def set_item_status(self, item_id: str, status_field_id: str, status_option_id: str) -> bool:
        """프로젝트 아이템의 상태를 설정합니다."""
        query = """
        mutation($project: ID!, $item: ID!, $field: ID!, $value: String!) {
            updateProjectV2ItemFieldValue(
                input: {
                    projectId: $project
                    itemId: $item
                    fieldId: $field
                    value: { singleSelectOptionId: $value }
                }
            ) {
                projectV2Item {
                    id
                }
            }
        }
        """
        
        project_info = self.get_project_info()
        if not project_info:
            return False
            
        variables = {
            "project": project_info['id'],
            "item": item_id,
            "field": status_field_id,
            "value": status_option_id
        }
        
        result = self._execute_graphql(query, variables)
        return bool(result)
    
    def get_issue_node_id(self, repo_owner: str, repo_name: str, issue_number: int) -> Optional[str]:
        """이슈의 node_id를 가져옵니다."""
        query = """
        query($owner: String!, $name: String!, $number: Int!) {
            repository(owner: $owner, name: $name) {
                issue(number: $number) {
                    id
                }
            }
        }
        """
        
        try:
            result = self._execute_graphql(query, {
                'owner': repo_owner,
                'name': repo_name,
                'number': issue_number
            })
            return result['repository']['issue']['id'] if result else None
        except Exception as e:
            logger.error(f"이슈 node_id 조회 중 오류 발생: {str(e)}")
            return None
    
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

TASK_CATEGORIES = {
    "기능 개발": {
        "emoji": "🔧",
        "name": "기능 개발",
        "description": "주요 기능 개발 태스크"
    },
    "UI/UX": {
        "emoji": "🎨",
        "name": "UI/UX",
        "description": "UI/UX 디자인 및 개선"
    },
    "QA/테스트": {
        "emoji": "🔍",
        "name": "QA/테스트",
        "description": "품질 보증 및 테스트"
    },
    "문서화": {
        "emoji": "📚",
        "name": "문서화",
        "description": "문서 작성 및 관리"
    },
    "유지보수": {
        "emoji": "🛠️",
        "name": "유지보수",
        "description": "버그 수정 및 유지보수"
    }
}

def find_report_issue(repo, project_name):
    """프로젝트 보고서 이슈를 찾습니다."""
    report_title = f"[{project_name}] 프로젝트 진행보고서"
    open_issues = repo.get_issues(state='open')
    for issue in open_issues:
        if issue.title == report_title:
            return issue
    return None

def get_assignees_string(issue):
    """이슈의 담당자 목록을 문자열로 반환합니다."""
    return ', '.join([assignee.login for assignee in issue.assignees]) if issue.assignees else 'TBD'

def get_task_duration(task_issue):
    """태스크의 예상 소요 시간을 계산합니다."""
    # 프로젝트 보드에서 Target Date 필드 확인
    github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
    project = GitHubProjectManager(github_token)
    items = project.get_project_items()
    
    if task_issue.number in items:
        item = items[task_issue.number]
        if item['target_date']:
            return item['target_date']
    
    # 기본값 반환
    return "1d"

def get_task_todos(project_items):
    """태스크별 투두 아이템들을 그룹화합니다."""
    task_todos = {}
    logger.info(f"\n=== 태스크 투두 그룹화 시작 ===")
    logger.info(f"총 {len(project_items)}개의 프로젝트 아이템 처리")
    
    for issue_number, item_data in project_items.items():
        category = None
        for label in item_data['labels']:
            if label.startswith('category:'):
                category = label.replace('category:', '').strip()
                logger.info(f"이슈 #{issue_number} - 카테고리 '{category}' 발견")
                break
        
        if category:
            if category not in task_todos:
                task_todos[category] = []
            task_todos[category].append({
                'number': issue_number,
                'title': item_data['title'],
                'status': item_data['status'],
                'closed_at': item_data['closed_at'],
                'assignees': item_data['assignees']
            })
            logger.info(f"이슈 #{issue_number} ({item_data['title']}) -> 카테고리 '{category}'에 추가됨")
    
    logger.info(f"\n카테고리별 투두 수:")
    for category, todos in task_todos.items():
        logger.info(f"- {category}: {len(todos)}개")
    
    return task_todos

def create_task_entry(task_issue, project_items):
    """태스크 항목을 생성합니다."""
    assignees = get_assignees_string(task_issue)
    title = task_issue.title
    issue_url = task_issue.html_url
    expected_time = get_task_duration(task_issue)
    
    # 해당 태스크의 투두 진행상황 계산
    task_todos = get_task_todos(project_items)
    todos = task_todos.get(title, [])
    total_todos = len(todos)
    completed_todos = sum(1 for todo in todos if todo['status'] == 'Done')
    progress = f"{(completed_todos/total_todos*100):.1f}%" if total_todos > 0 else "0%"
    
    status = "🟡 진행중" if completed_todos < total_todos else "✅ 완료" if total_todos > 0 else "⬜ 대기중"
    
    return f"| [TSK-{task_issue.number}]({issue_url}) | {title} | {assignees} | {expected_time} | - | {status} ({progress}) | - |"

def get_category_from_labels(issue_labels):
    """이슈의 라벨을 기반으로 카테고리를 결정합니다."""
    for label in issue_labels:
        if label.name.startswith("category:"):
            return label.name.replace("category:", "").strip()
    return "기타"  # 기본값

def create_category_sections():
    """모든 카테고리 섹션을 생성합니다."""
    sections = []
    for category_key, category_info in TASK_CATEGORIES.items():
        section = f"""<details>
<summary><h3>{TASK_CATEGORIES[category_key]['emoji']} {category_key}</h3></summary>

| 태스크 ID | 태스크명 | 담당자 | 예상 시간 | 실제 시간 | 진행 상태 | 우선순위 |
| --------- | -------- | ------ | --------- | --------- | --------- | -------- |

</details>"""
        sections.append(section)
    return "\n\n".join(sections)

def update_report_content(old_content, new_task_entry, category_key):
    """보고서 내용을 업데이트합니다."""
    # 카테고리 섹션 찾기
    category_start = old_content.find(f"<h3>{TASK_CATEGORIES[category_key]['emoji']} {category_key}</h3>")
    if category_start == -1:
        return old_content
    
    # 테이블 찾기
    table_header = "| 태스크 ID | 태스크명 | 담당자 | 예상 시간 | 실제 시간 | 진행 상태 | 우선순위 |"
    header_pos = old_content.find(table_header, category_start)
    if header_pos == -1:
        return old_content
    
    # 테이블 끝 찾기
    table_end = old_content.find("</details>", header_pos)
    if table_end == -1:
        return old_content
    
    # 현재 테이블 내용 가져오기
    table_content = old_content[header_pos:table_end].strip()
    lines = table_content.split('\n')
    
    # 새 태스크 항목 추가 또는 업데이트
    task_number = re.search(r'TSK-(\d+)', new_task_entry).group(1)
    task_exists = False
    
    for i, line in enumerate(lines):
        if f"TSK-{task_number}" in line:
            lines[i] = new_task_entry
            task_exists = True
            break
    
    if not task_exists:
        if len(lines) > 2:
            lines.append(new_task_entry)
        else:
            lines = [table_header, "| --------- | -------- | ------ | --------- | --------- | --------- | -------- |", new_task_entry]
    
    # 새 테이블 생성
    new_table = '\n'.join(lines)
    
    return f"{old_content[:header_pos]}{new_table}\n\n{old_content[table_end:]}"

def create_task_history_section(project_items):
    """태스크 히스토리 섹션을 생성합니다."""
    logger.info("\n=== 태스크 히스토리 섹션 생성 시작 ===")
    task_todos = get_task_todos(project_items)
    history_items = {}  # 날짜별로 그룹화
    
    logger.info("\n완료된 투두 처리:")
    for task_name, todos in task_todos.items():
        for todo in todos:
            if todo['status'] == 'Done' and todo['closed_at']:
                closed_date = datetime.fromisoformat(todo['closed_at'].replace('Z', '+00:00')).strftime('%Y-%m-%d')
                logger.info(f"완료된 투두 발견: #{todo['number']} - {todo['title']} (완료일: {closed_date}, 상위 태스크: {task_name})")
                
                if closed_date not in history_items:
                    history_items[closed_date] = []
                    
                history_items[closed_date].append({
                    'number': todo['number'],
                    'title': todo['title'],
                    'category': task_name
                })
    
    if not history_items:
        return """## 📅 태스크 완료 히스토리

아직 완료된 태스크가 없습니다."""
    
    history_section = "## 📅 태스크 완료 히스토리\n\n"
    
    # 날짜별로 정렬 (최신순)
    sorted_dates = sorted(history_items.keys(), reverse=True)
    
    for date in sorted_dates:
        items = history_items[date]
        history_section += f"""<details>
<summary><h3 style="display: inline;">📆 {date} ({len(items)}개)</h3></summary>

| 투두 ID | 투두명 | 상위 태스크 |
| ------- | ------ | ----------- |
"""
        for item in items:
            history_section += f"| #{item['number']} | {item['title']} | {item['category']} |\n"
        
        history_section += "\n</details>\n\n"
    
    logger.info(f"\n총 {sum(len(items) for items in history_items.values())}개의 완료된 투두 기록됨")
    return history_section

def create_report_body(project_name, project=None):
    """프로젝트 보고서 템플릿을 생성합니다."""
    category_sections = create_category_sections()
    
    # 프로젝트 상태 가져오기
    if project is None:
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        project = GitHubProjectManager(github_token)
    project_items = project.get_project_items()
    
    # 카테고리별 통계 초기화
    category_stats = {}
    
    # 프로젝트 아이템 처리
    for item_data in project_items.values():
        category = item_data['category']
        if category not in category_stats:
            category_stats[category] = {'total': 0, 'completed': 0, 'in_progress': 0}
        
        category_stats[category]['total'] += 1
        if item_data['status'] == 'Done':
            category_stats[category]['completed'] += 1
        elif item_data['status'] == 'In Progress':
            category_stats[category]['in_progress'] += 1
    
    # 진행 현황 섹션 생성
    progress_section = create_progress_section_from_project(category_stats)
    
    # 히스토리 섹션 생성
    history_section = create_task_history_section(project_items)
    
    return f"""<div align="center">

![header](https://capsule-render.vercel.app/api?type=transparent&color=39FF14&height=150&section=header&text=Project%20Report&fontSize=50&animation=fadeIn&fontColor=39FF14&desc=프로젝트%20진행%20보고서&descSize=25&descAlignY=75)

# 📊 프로젝트 진행보고서

</div>

## 📌 기본 정보

**프로젝트명**: {project_name}  
**보고서 작성일**: {datetime.now().strftime('%Y-%m-%d')}  
**보고 기간**: {datetime.now().strftime('%Y-%m-%d')} ~ 진행중

## 📋 태스크 상세 내역

{category_sections}

## 📊 진행 현황 요약

{progress_section}

{history_section}

## 📝 특이사항 및 리스크

| 구분 | 내용 | 대응 방안 |
| ---- | ---- | --------- |
| - | - | - |

## 📈 다음 단계 계획

1. 초기 설정 및 환경 구성
2. 세부 작업 항목 정의
3. 진행 상황 정기 업데이트

---
> 이 보고서는 자동으로 생성되었으며, 담당자가 지속적으로 업데이트할 예정입니다.
"""

def process_approval(issue, repo):
    """이슈의 라벨에 따라 승인 처리를 수행합니다."""
    labels = [label.name for label in issue.labels]
    project_name = repo.name
    
    if '✅ 승인완료' in labels:
        # GitHubProjectManager 인스턴스 생성 및 프로젝트 아이템 가져오기
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        project = GitHubProjectManager(github_token)
        project_items = project.get_project_items()
        
        # 태스크 카테고리 결정
        category_key = get_category_from_labels(issue.labels)
        
        # 보고서 이슈 찾기
        report_issue = find_report_issue(repo, project_name)
        
        if report_issue:
            # 태스크 항목 생성 및 보고서 업데이트
            task_entry = create_task_entry(issue, project_items)
            updated_body = update_report_content(report_issue.body, task_entry, category_key)
            updated_body = update_task_progress_in_report(updated_body)
            report_issue.edit(body=updated_body)
            report_issue.create_comment(f"✅ 태스크 #{issue.number}이 {category_key} 카테고리에 추가되었습니다.")
            
            # 프로젝트 보드에 추가
            node_id = project.get_issue_node_id(repo.owner.login, repo.name, issue.number)
            if node_id:
                project.add_issue_to_project(node_id)
        else:
            # 새 보고서 이슈 생성
            report_body = create_report_body(project_name, project)
            new_issue = repo.create_issue(
                title=f"[{project_name}] 프로젝트 진행보고서",
                body=report_body,
                labels=['📊 진행중']
            )
            task_entry = create_task_entry(issue, project_items)
            updated_body = update_report_content(report_body, task_entry, category_key)
            report_issue = new_issue
            report_issue.edit(body=updated_body)
        
        issue.create_comment("✅ 태스크가 승인되어 보고서에 추가되었습니다.")
        
    elif '❌ 반려' in labels:
        issue.create_comment("❌ 태스크가 반려되었습니다. 수정 후 다시 제출해주세요.")
        
    elif '⏸️ 보류' in labels:
        issue.create_comment("⏸️ 태스크가 보류되었습니다. 추가 논의가 필요합니다.")

def update_task_progress_in_report(body, project=None):
    """보고서의 태스크 진행률을 업데이트합니다."""
    logger.info("\n=== 태스크 진행률 업데이트 시작 ===")
    if project is None:
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        project = GitHubProjectManager(github_token)
    project_items = project.get_project_items()
    
    # 카테고리별 통계 초기화
    category_stats = {}
    
    # 프로젝트 아이템 처리
    logger.info("\n프로젝트 아이템 상태 분석:")
    for item_data in project_items.values():
        category = item_data['category']
        if category not in category_stats:
            category_stats[category] = {'total': 0, 'completed': 0, 'in_progress': 0}
        
        category_stats[category]['total'] += 1
        if item_data['status'] == 'Done':
            category_stats[category]['completed'] += 1
            logger.info(f"완료된 태스크 발견: #{item_data['number']} - {item_data['title']} ({category})")
        elif item_data['status'] == 'In Progress':
            category_stats[category]['in_progress'] += 1
            logger.info(f"진행중인 태스크 발견: #{item_data['number']} - {item_data['title']} ({category})")
    
    # 보고서 내용 업데이트
    logger.info("\n보고서 섹션 업데이트 시작")
    
    # 히스토리 섹션 업데이트
    logger.info("히스토리 섹션 업데이트 중...")
    history_section = create_task_history_section(project_items)
    
    # 보고서 섹션 분리 및 업데이트
    sections = {}
    current_section = None
    current_content = []
    
    for line in body.split('\n'):
        if line.startswith('## '):
            if current_section:
                sections[current_section] = '\n'.join(current_content)
            current_section = line[3:].strip()
            current_content = [line]
        elif line.startswith('---'):
            break  # footer 시작 부분에서 중단
        else:
            if current_section:
                current_content.append(line)
    
    if current_section:
        sections[current_section] = '\n'.join(current_content)
    
    # 히스토리 섹션 교체
    sections['📅 태스크 완료 히스토리'] = history_section
    
    # 진행 현황 섹션 업데이트
    progress_section = create_progress_section_from_project(category_stats)
    if '📊 진행 현황 요약' in sections:
        sections['📊 진행 현황 요약'] = f"## 📊 진행 현황 요약\n\n{progress_section}"
    
    # 섹션 순서 정의
    section_order = [
        '📌 기본 정보',
        '📋 태스크 상세 내역',
        '📊 진행 현황 요약',
        '📅 태스크 완료 히스토리',
        '📝 특이사항 및 리스크'
    ]
    
    # 보고서 재구성
    updated_body = []
    for section in section_order:
        if section in sections:
            updated_body.append(sections[section])
    
    # 마지막 줄 한 번만 추가
    updated_body.append("\n---\n> 이 보고서는 자동으로 생성되었으며, 담당자가 지속적으로 업데이트할 예정입니다.")
    
    return '\n\n'.join(updated_body)

def create_progress_section_from_project(category_stats):
    """프로젝트 보드 데이터를 기반으로 진행 현황 섹션을 생성합니다."""
    # 전체 태스크 수 계산
    total_tasks = 0
    total_completed = 0
    total_in_progress = 0
    
    # 모든 카테고리의 통계 합산
    for stats in category_stats.values():
        total_tasks += stats['total']
        total_completed += stats['completed']
        total_in_progress += stats['in_progress']
    
    # 기본 진행률 섹션 생성
    progress_summary = "### 전체 진행률\n\n"
    
    if total_tasks == 0:
        progress_summary += "아직 등록된 태스크가 없습니다.\n"
        completed_percent = 0
        in_progress_percent = 0
        waiting_percent = 100
    else:
        completed_percent = (total_completed / total_tasks) * 100
        in_progress_percent = (total_in_progress / total_tasks) * 100
        waiting_percent = ((total_tasks - total_completed - total_in_progress) / total_tasks) * 100
        progress_summary += f"전체 진행 상태: {total_completed}/{total_tasks} 완료 ({completed_percent:.1f}%)\n\n"
        progress_summary += """```mermaid
pie title 전체 진행 현황
    "완료" : """ + f"{completed_percent:.1f}" + """
    "진행중" : """ + f"{in_progress_percent:.1f}" + """
    "대기중" : """ + f"{waiting_percent:.1f}" + """
```\n"""

    # 상세 진행 현황 차트 추가
    progress_summary += f"""
<details>
<summary><h4 style="display: inline;">📊 카테고리별 진행 현황</h4></summary>

| 태스크명 | 완료 | 진행중 | 대기중 | 진행률 |
|----------|------|--------|--------|---------|"""

    # 태스크별 상세 현황 추가
    has_tasks = False
    for category, stats in category_stats.items():
        if stats['total'] > 0:
            has_tasks = True
            cat_completed = (stats['completed'] / stats['total']) * 100
            cat_waiting = stats['total'] - stats['completed'] - stats['in_progress']
            
            # 대괄호 안의 시스템 이름 추출
            task_name = re.match(r'\[(.*?)\]', category)
            display_name = task_name.group(1) if task_name else category
            
            progress_summary += f"\n| {display_name} | {stats['completed']} | {stats['in_progress']} | {cat_waiting} | {cat_completed:.1f}% |"
    
    if not has_tasks:
        progress_summary += "\n| - | - | - | - | - |"
    
    progress_summary += "\n\n</details>"
    
    # 일자별 진행 현황 추가
    current_date = datetime.now().strftime('%Y-%m-%d')
    progress_summary += f"""

<details>
<summary><h4 style="display: inline;">📅 일자별 상세 현황</h4></summary>

| 날짜 | 완료된 태스크 | 신규 태스크 | 진행중 태스크 |
|------|--------------|-------------|--------------|
| {current_date} | {total_completed} | {total_tasks} | {total_in_progress} |

</details>"""
    
    return progress_summary

def main():
    try:
        # PAT를 우선적으로 사용
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        if not github_token:
            raise ValueError("GitHub 토큰이 설정되지 않았습니다.")
        
        logger.info("GitHub 토큰 확인 완료")
        
        g = Github(github_token)
        repo_name = os.environ.get('GITHUB_REPOSITORY')
        if not repo_name:
            raise ValueError("GitHub 저장소 정보를 찾을 수 없습니다.")
            
        logger.info(f"Processing repository: {repo_name}")
        
        try:
            repo = g.get_repo(repo_name)
            logger.info("저장소 접근 성공")
            project_name = repo.name
            logger.info(f"프로젝트 이름: {project_name}")
            
            # GitHubProjectManager 인스턴스 생성 (한 번만)
            project = GitHubProjectManager(github_token)
            project_items = project.get_project_items()
            logger.info(f"프로젝트 아이템 {len(project_items)}개 확인됨")
            
            # 보고서 업데이트 또는 생성
            report_issue = find_report_issue(repo, project_name)
            if report_issue:
                logger.info(f"기존 보고서 #{report_issue.number} 업데이트 중")
                # 히스토리 섹션 업데이트
                logger.info("태스크 진행률 업데이트 시작")
                updated_body = update_task_progress_in_report(report_issue.body, project)
                report_issue.edit(body=updated_body)
                logger.info("보고서 업데이트 완료")
            else:
                logger.info("새 보고서 생성 중")
                report_body = create_report_body(project_name, project)
                new_issue = repo.create_issue(
                    title=f"[{project_name}] 프로젝트 진행보고서",
                    body=report_body,
                    labels=['📊 진행중']
                )
                logger.info(f"새 보고서 생성 완료: #{new_issue.number}")
            
        except Exception as e:
            logger.error(f"저장소 또는 프로젝트 처리 실패: {str(e)}")
            raise
        
    except Exception as e:
        logger.error(f"오류 발생: {str(e)}")
        logger.error(f"오류 상세: {type(e).__name__}")
        raise

if __name__ == '__main__':
    main() 