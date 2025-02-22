import os
import json
import logging
from github import Github
import requests
import re

# 로깅 설정
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def get_project_v2(github_token, org_name, project_number):
    """GitHub Projects v2 접근을 위한 GraphQL 쿼리"""
    headers = {
        "Authorization": f"Bearer {github_token}",
        "Accept": "application/vnd.github.v3+json"
    }
    
    query = """
    query($org: String!, $number: Int!) {
        organization(login: $org) {
            projectV2(number: $number) {
                id
                title
                url
            }
        }
    }
    """
    
    variables = {
        "org": org_name,
        "number": project_number
    }
    
    try:
        response = requests.post(
            'https://api.github.com/graphql',
            json={'query': query, 'variables': variables},
            headers=headers
        )
        response.raise_for_status()
        result = response.json()
        
        if 'errors' in result:
            logger.error(f"GraphQL errors: {result['errors']}")
            return None
            
        return result['data']['organization']['projectV2']
    except Exception as e:
        logger.error(f"Error fetching project v2: {str(e)}")
        return None

def add_issue_to_project_v2(github_token, project_id, issue_node_id):
    """이슈를 프로젝트에 추가"""
    headers = {
        "Authorization": f"Bearer {github_token}",
        "Accept": "application/vnd.github.v3+json"
    }
    
    mutation = """
    mutation($project: ID!, $issue: ID!) {
        addProjectV2ItemById(input: {projectId: $project, contentId: $issue}) {
            item {
                id
            }
        }
    }
    """
    
    variables = {
        "project": project_id,
        "issue": issue_node_id
    }
    
    try:
        response = requests.post(
            'https://api.github.com/graphql',
            json={'query': mutation, 'variables': variables},
            headers=headers
        )
        response.raise_for_status()
        return response.json()
    except Exception as e:
        logger.error(f"Error adding issue to project: {str(e)}")
        return None

def extract_issue_numbers(text):
    """이슈 본문에서 `- #숫자` 형식의 이슈 참조만 추출"""
    pattern = r'-\s*#(\d+)'
    matches = re.finditer(pattern, text)
    return [match.group(1) for match in matches]

def update_project_board():
    try:
        github_token = os.environ.get("PAT") or os.environ["GITHUB_TOKEN"]
        g = Github(github_token)
        
        with open(os.environ["GITHUB_EVENT_PATH"]) as f:
            event = json.load(f)
        
        # DSR 이슈 확인
        issue = event.get("issue", {})
        if not issue or "Development Status Report" not in issue.get("title", ""):
            logger.info("Not a DSR issue update, skipping")
            return
            
        # 이미 처리된 이슈인지 확인
        if any(label["name"] == "in-project" for label in issue.get("labels", [])):
            logger.info("DSR issue already processed")
            return
            
        repo = g.get_repo(os.environ["GITHUB_REPOSITORY"])
        
        # Projects v2 접근
        org_name = "KGAMeta8thTeam1"  # 조직 이름
        project_number = 2  # 프로젝트 번호
        
        project = get_project_v2(github_token, org_name, project_number)
        if not project:
            logger.error("Could not find project v2")
            return
            
        # DSR 이슈 본문에서 이슈 참조 추출
        issue_numbers = extract_issue_numbers(issue.get("body", ""))
        logger.info(f"Found {len(issue_numbers)} issue references in DSR")
        
        for issue_number in issue_numbers:
            try:
                target_issue = repo.get_issue(int(issue_number))
                
                # 이미 프로젝트에 추가된 이슈는 건너뛰기
                if any(label.name == "in-project" for label in target_issue.labels):
                    logger.info(f"Issue #{issue_number} already in project")
                    continue
                
                # 이슈의 node_id 가져오기
                issue_response = requests.get(
                    f"https://api.github.com/repos/{repo.full_name}/issues/{issue_number}",
                    headers={"Authorization": f"Bearer {github_token}"}
                )
                issue_response.raise_for_status()
                node_id = issue_response.json()['node_id']
                
                # 프로젝트에 이슈 추가
                result = add_issue_to_project_v2(github_token, project['id'], node_id)
                if result and 'errors' not in result:
                    target_issue.add_to_labels("in-project")
                    logger.info(f"Added issue #{issue_number} to project")
                
            except Exception as e:
                logger.error(f"Failed to process issue #{issue_number}: {str(e)}")
        
        # DSR 이슈에 처리 완료 표시
        dsr_issue = repo.get_issue(issue["number"])
        dsr_issue.add_to_labels("in-project")
        
    except Exception as e:
        logger.error(f"Failed to update project board: {str(e)}")
        raise

def handle_card_movement(card_event, columns, repo):
    """카드 이동 처리"""
    logger.info(f"Processing card movement event: {card_event['id']}")
    if card_event["column_id"]:
        logger.debug(f"Card moved to column ID: {card_event['column_id']}")
        issue = get_issue_from_card(card_event, repo)
        if issue:
            logger.info(f"Updating status for issue #{issue.number}")
            update_issue_status(issue, card_event["column_id"], columns)
        else:
            logger.debug("No associated issue found for card")

def handle_issue_status(issue, columns):
    """이슈 상태 변경 처리"""
    logger.info(f"Processing issue status: #{issue['number']}")
    if any(label["name"] == "done" for label in issue["labels"]):
        logger.info("Moving issue to Done column")
        move_to_column(issue, columns["Done"])
    elif any(label["name"] == "in-progress" for label in issue["labels"]):
        logger.info("Moving issue to In Progress column")
        move_to_column(issue, columns["In Progress"])
    else:
        logger.info("Moving issue to To Do column")
        move_to_column(issue, columns["To Do"])

def get_issue_from_card(card, repo):
    """카드에서 이슈 정보 추출"""
    logger.debug(f"Extracting issue from card: {card['id']}")
    if "content_url" in card:
        issue_url = card["content_url"]
        issue_number = int(issue_url.split("/")[-1])
        logger.debug(f"Found issue #{issue_number}")
        return repo.get_issue(issue_number)
    logger.debug("No content URL found in card")
    return None

def move_to_column(issue, column):
    """이슈를 지정된 칼럼으로 이동"""
    logger.info(f"Moving issue #{issue.number} to column: {column.name}")
    for card in column.get_cards():
        if card.content_url == issue.url:
            logger.debug("Issue already in target column")
            return
    column.create_card(content_id=issue.id, content_type="Issue")
    logger.info(f"Created new card for issue #{issue.number} in column {column.name}")

def update_issue_status(issue, column_id, columns):
    """이슈 상태 업데이트"""
    logger.info(f"Updating status for issue #{issue.number}")
    status_labels = {
        columns["To Do"].id: "todo",
        columns["In Progress"].id: "in-progress",
        columns["Done"].id: "done"
    }
    
    if column_id in status_labels:
        logger.debug(f"Updating labels for column ID: {column_id}")
        for label in issue.labels:
            if label.name in status_labels.values():
                logger.debug(f"Removing label: {label.name}")
                issue.remove_from_labels(label.name)
        
        new_label = status_labels[column_id]
        logger.debug(f"Adding new label: {new_label}")
        issue.add_to_labels(new_label)
    else:
        logger.warning(f"Unknown column ID: {column_id}")

if __name__ == "__main__":
    update_project_board() 