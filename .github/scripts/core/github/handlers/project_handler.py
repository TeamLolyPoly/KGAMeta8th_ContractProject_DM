"""
GitHub 프로젝트 관리 핸들러
"""
import os
import logging
from typing import Dict, Optional
from ..client import GitHubClient
import re
from datetime import datetime
from ...task.models.status import TaskState

logger = logging.getLogger(__name__)

class GitHubProjectHandler:
    def __init__(self, client: GitHubClient, project_number: int = None):
        self.client = client
        self.project_number = self._init_project_number(project_number)

    def _init_project_number(self, project_number: Optional[int]) -> int:
        """프로젝트 번호를 초기화합니다."""
        projects = self.list_projects()
        if projects:
            logger.info(f"사용 가능한 프로젝트 목록:")
            for p in projects:
                logger.info(f"  - #{p['number']}: {p['title']}")
            
            if project_number and any(p['number'] == project_number for p in projects):
                return project_number
            else:
                project_number = projects[0]['number']
                logger.info(f"프로젝트 번호 자동 설정: #{project_number}")
                return project_number
        else:
            project_number = project_number or int(os.environ.get('PROJECT_NUMBER', '1'))
            logger.warning(f"프로젝트 목록을 가져올 수 없어 기본값 사용: #{project_number}")
            return project_number

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
                            ... on ProjectV2Field {
                                id
                                name
                            }
                            ... on ProjectV2SingleSelectField {
                                id
                                name
                                options {
                                    id
                                    name
                                }
                            }
                            ... on ProjectV2IterationField {
                                id
                                name
                                configuration {
                                    iterations {
                                        id
                                        title
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.client.org,
            "number": self.project_number
        }
        
        result = self.client._execute_graphql(query, variables)
        if not result:
            logger.error("프로젝트 정보를 가져오는데 실패했습니다.")
            return None
            
        project_data = result.get('organization', {}).get('projectV2')
        if not project_data:
            logger.error("프로젝트 데이터가 없습니다.")
            return None
            
        return project_data

    def get_project_items(self) -> Dict:
        """프로젝트 아이템들을 가져옵니다."""
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
        
        result = self.client._execute_graphql(query, {
            "org": self.client.org,
            "number": self.project_number
        })
        
        if not result:
            logger.error("프로젝트 아이템을 가져오는데 실패했습니다.")
            return {}
        
        items = self._process_project_items(result)
        logger.info(f"총 {len(items)}개의 아이템을 가져왔습니다.")
        return items

    def _process_project_items(self, result: Dict) -> Dict[int, Dict]:
        """GraphQL 결과를 처리하여 아이템 정보를 구성합니다."""
        logger.debug("GraphQL 응답 처리 시작")
        
        items = {}
        for node in result['organization']['projectV2']['items']['nodes']:
            if not node['content']:
                logger.debug("컨텐츠가 없는 노드 발견, 건너뜀")
                continue
            
            issue = node['content']
            item_data = {
                'id': node['id'],
                'number': issue['number'],
                'title': issue['title'],
                'url': issue['url'],
                'state': issue['state'],
                'created_at': issue['createdAt'],
                'closed_at': issue['closedAt'],
                'labels': [label['name'] for label in issue['labels']['nodes']],
                'assignees': [
                    {'login': assignee['login']}
                    for assignee in issue['assignees']['nodes']
                ],
                'fields': {}
            }
            
            # 필드 값 처리
            for field_value in node['fieldValues']['nodes']:
                if not field_value or 'field' not in field_value:
                    continue
                
                field_name = field_value['field']['name']
                if 'name' in field_value:  # SingleSelectValue
                    item_data['fields'][field_name] = field_value['name']
                elif 'date' in field_value:  # DateValue
                    item_data['fields'][field_name] = field_value['date']
                elif 'number' in field_value:  # NumberValue
                    item_data['fields'][field_name] = field_value['number']
            
            items[issue['number']] = item_data
            
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
        
        result = self.client._execute_graphql(query, {"org": self.client.org})
        if not result or 'organization' not in result:
            logger.error(f"프로젝트 목록 조회 실패: {result}")
            return []
            
        return result['organization']['projectsV2']['nodes']

    def get_task_issues(self) -> Dict:
        """태스크 이슈들을 가져옵니다."""
        logger.info("태스크 이슈 조회 시작")
        
        query = """
        query($org: String!, $number: Int!) {
            organization(login: $org) {
                projectV2(number: $number) {
                    items(first: 100) {
                        nodes {
                            content {
                                ... on Issue {
                                    number
                                    title
                                    body
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
        
        result = self.client._execute_graphql(query, {
            "org": self.client.org,
            "number": self.project_number
        })
        
        if not result:
            logger.error("태스크 이슈를 가져오는데 실패했습니다.")
            return {}
        
        tasks = {}
        for node in result['organization']['projectV2']['items']['nodes']:
            if not node['content']:
                continue
                
            issue = node['content']
            task_match = re.match(r'\[(.*?)\]', issue['title'])
            if not task_match:
                continue
                
            task_name = task_match.group(1)
            tasks[task_name] = {
                'number': issue['number'],
                'title': task_name,
                'state': issue['state'],
                'created_at': issue['createdAt'],
                'closed_at': issue['closedAt'],
                'labels': [label['name'] for label in issue['labels']['nodes']],
                'assignees': [
                    {'login': assignee['login']}
                    for assignee in issue['assignees']['nodes']
                ],
                'expected_time': self._extract_expected_time(issue['body'])
            }
        
        logger.info(f"총 {len(tasks)}개의 태스크 이슈를 가져왔습니다.")
        return tasks

    def _extract_expected_time(self, body: str) -> str:
        """이슈 본문에서 예상 소요 시간을 추출합니다."""
        if not body:
            return '-'
            
        time_match = re.search(r'예상\s*소요\s*시간[:\s]*([^\n]+)', body)
        if time_match:
            return time_match.group(1).strip()
        return '-'

    def _get_repository_id(self, repo_name: str) -> Optional[str]:
        """저장소의 ID를 가져옵니다."""
        query = """
        query($org: String!, $name: String!) {
            organization(login: $org) {
                repository(name: $name) {
                    id
                    labels(first: 100) {
                        nodes {
                            id
                            name
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.client.org,
            "name": repo_name
        }
        
        result = self.client._execute_graphql(query, variables)
        if result and 'organization' in result and 'repository' in result['organization']:
            repo = result['organization']['repository']
            labels = {
                label['name']: label['id']
                for label in repo['labels']['nodes']
            }
            
            # report 라벨이 없으면 생성
            if 'report' not in labels:
                logger.info("'report' 라벨 생성 시도...")
                create_label_mutation = """
                mutation($repositoryId: ID!, $name: String!, $description: String!, $color: String!) {
                    createLabel(input: {
                        repositoryId: $repositoryId,
                        name: $name,
                        description: $description,
                        color: "0E8A16"
                    }) {
                        label {
                            id
                        }
                    }
                }
                """
                
                label_variables = {
                    "repositoryId": repo['id'],
                    "name": "report",
                    "description": "프로젝트 보고서 관련 이슈",
                    "color": "0E8A16"  # 초록색
                }
                
                label_result = self.client._execute_graphql(create_label_mutation, label_variables)
                if label_result and 'createLabel' in label_result:
                    labels['report'] = label_result['createLabel']['label']['id']
                    logger.info("'report' 라벨이 성공적으로 생성되었습니다.")
                else:
                    logger.error("'report' 라벨 생성 실패")
            
            return repo['id'], labels
        return None, {}

    def _create_report_label(self, repo_id: str) -> Optional[str]:
        """report 라벨을 생성합니다."""
        create_label_mutation = """
        mutation($repositoryId: ID!, $name: String!, $description: String!, $color: String!) {
            createLabel(input: {
                repositoryId: $repositoryId,
                name: $name,
                description: $description,
                color: $color
            }) {
                label {
                    id
                }
            }
        }
        """
        
        variables = {
            "repositoryId": repo_id,
            "name": "report",
            "description": "프로젝트 보고서 관련 이슈",
            "color": "0E8A16"  # 초록색
        }
        
        result = self.client._execute_graphql(create_label_mutation, variables)
        if result and 'createLabel' in result:
            label_id = result['createLabel']['label']['id']
            logger.info("'report' 라벨이 성공적으로 생성되었습니다.")
            return label_id
        else:
            logger.error("'report' 라벨 생성 실패")
            return None

    def create_or_update_report(self, project_name: str, report_formatter) -> None:
        """프로젝트 보고서를 생성하거나 업데이트합니다."""
        logger.info("프로젝트 보고서 생성/업데이트 시작")
        
        # 저장소 ID와 라벨 ID 가져오기
        repo_id, labels = self._get_repository_id(project_name)
        if not repo_id:
            logger.error("저장소 ID를 가져오는데 실패했습니다.")
            return
        
        # report 라벨이 없으면 생성
        if 'report' not in labels:
            logger.info("'report' 라벨이 없어 새로 생성합니다...")
            label_id = self._create_report_label(repo_id)
            if label_id:
                labels['report'] = label_id
        
        # 보고서 제목 생성
        current_date = datetime.now().strftime('%Y-%m-%d')
        report_title = f"📊 프로젝트 진행 보고서 ({current_date}) - {project_name}"
        
        # 보고서 본문 생성
        report_body = report_formatter.format_report()
        
        # 기존 보고서 찾기
        query = """
        query($org: String!, $name: String!) {
            organization(login: $org) {
                repository(name: $name) {
                    issues(first: 10, states: OPEN, labels: ["report"], orderBy: {field: CREATED_AT, direction: DESC}) {
                        nodes {
                            id
                            number
                            title
                            createdAt
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.client.org,
            "name": project_name
        }
        
        result = self.client._execute_graphql(query, variables)
        
        existing_report = None
        if result and 'organization' in result and 'repository' in result['organization']:
            issues = result['organization']['repository']['issues']['nodes']
            today = datetime.now().strftime('%Y-%m-%d')
            expected_title = f"📊 프로젝트 진행 보고서 ({today})"
            
            for issue in issues:
                issue_date = datetime.fromisoformat(issue['createdAt'].replace('Z', '+00:00')).strftime('%Y-%m-%d')
                if issue_date == today and expected_title in issue['title']:
                    existing_report = issue
                    logger.info(f"오늘자 보고서 #{issue['number']} 발견: {issue['title']}")
                    break
                else:
                    logger.debug(f"보고서 제외 #{issue['number']}: {issue['title']} (생성일: {issue_date})")
        
        if existing_report:
            # 기존 보고서 업데이트
            update_query = """
            mutation($id: ID!, $title: String!, $body: String!) {
                updateIssue(input: {id: $id, title: $title, body: $body}) {
                    issue {
                        number
                    }
                }
            }
            """
            
            variables = {
                "id": existing_report['id'],
                "title": report_title,
                "body": report_body
            }
            
            result = self.client._execute_graphql(update_query, variables)
            if result:
                logger.info(f"보고서 #{existing_report['number']} 업데이트 완료")
            else:
                logger.error("보고서 업데이트 실패")
        else:
            # 보고서 생성
            create_query = """
            mutation($repositoryId: ID!, $title: String!, $body: String!, $labelIds: [ID!]) {
                createIssue(input: {
                    repositoryId: $repositoryId,
                    title: $title,
                    body: $body,
                    labelIds: $labelIds
                }) {
                    issue {
                        number
                    }
                }
            }
            """
            
            label_ids = []
            if 'report' in labels:
                label_ids.append(labels['report'])
            else:
                logger.warning("'report' 라벨을 찾을 수 없습니다.")
            
            variables = {
                "repositoryId": repo_id,
                "title": report_title,
                "body": report_body,
                "labelIds": label_ids
            }
            
            result = self.client._execute_graphql(create_query, variables)
            if result and 'createIssue' in result:
                issue_number = result['createIssue']['issue']['number']
                logger.info(f"새 보고서 #{issue_number} 생성 완료")
            else:
                logger.error(f"보고서 생성 실패: {result}")

    def update_project_status(self, task_manager) -> None:
        """프로젝트의 상태를 업데이트합니다."""
        logger.info("프로젝트 상태 업데이트 시작")
        
        # 프로젝트 필드 정보 가져오기
        project_info = self.get_project_info()
        if not project_info:
            logger.error("프로젝트 정보를 가져오는데 실패했습니다.")
            return
            
        # 상태 필드 찾기
        status_field = None
        for field in project_info['fields']['nodes']:
            if isinstance(field, dict) and field.get('name') == 'Status' and 'options' in field:
                status_field = field
                break
                
        if not status_field:
            logger.error("Status 필드를 찾을 수 없습니다.")
            return
            
        # 상태 옵션 매핑
        status_options = {
            option['name']: option['id']
            for option in status_field['options']
        }
        
        # 각 아이템의 상태 업데이트
        project_items = self.get_project_items()
        for item_number, item_data in project_items.items():
            task_name = None
            task_match = re.match(r'\[(.*?)\]', item_data['title'])
            if task_match:
                task_name = task_match.group(1)
            
            if not task_name:
                continue
            
            # 현재 아이템의 상태 확인
            current_status = item_data.get('fields', {}).get('Status', 'Todo')
            
            # 이슈가 닫혀있거나 현재 Done 상태면 건너뛰기
            if item_data['state'] == 'CLOSED' or current_status == 'Done':
                logger.debug(f"아이템 #{item_number} ({task_name})는 이미 완료됨")
                continue
                
            # 태스크 상태 확인
            task_status = task_manager.get_task_status(task_name)
            if not task_status:
                continue
                
            # 상태에 따른 옵션 ID 결정
            status_name = None
            if task_status.state == TaskState.COMPLETED:
                status_name = "Done"
            elif task_status.state == TaskState.IN_PROGRESS:
                status_name = "In Progress"
            elif task_status.state == TaskState.BLOCKED:
                status_name = "Blocked"
            else:
                status_name = "Todo"
            
            # 현재 상태와 같으면 업데이트하지 않음
            if status_name == current_status:
                logger.debug(f"아이템 #{item_number} ({task_name})는 이미 {status_name} 상태")
                continue
            
            if status_name not in status_options:
                logger.error(f"'{status_name}' 상태 옵션을 찾을 수 없습니다.")
                continue
            
            # 상태 업데이트
            mutation = """
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
            
            variables = {
                "project": project_info['id'],
                "item": item_data['id'],
                "field": status_field['id'],
                "value": status_options[status_name]
            }
            
            try:
                result = self.client._execute_graphql(mutation, variables)
                if result:
                    logger.info(f"아이템 #{item_number} ({task_name}) 상태 업데이트: {current_status} -> {status_name}")
                else:
                    logger.error(f"아이템 #{item_number} 상태 업데이트 실패")
            except Exception as e:
                logger.error(f"아이템 #{item_number} 상태 업데이트 중 오류 발생: {str(e)}") 