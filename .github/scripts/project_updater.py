import os
import json
import logging
from github import Github
import requests

# 로깅 설정
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

def get_project_v2(github_token, org_name, project_number):
    """GitHub Projects v2 접근을 위한 GraphQL 쿼리"""
    headers = {
        "Authorization": f"Bearer {github_token}",
        "Accept": "application/vnd.github.v3+json"
    }
    
    # GraphQL 쿼리
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
                    }
                }
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

def update_project_board():
    """메인 함수 개선"""
    try:
        logger.info("Starting project board update")
        github_token = os.environ.get("PAT") or os.environ["GITHUB_TOKEN"]
        g = Github(github_token)
        
        with open(os.environ["GITHUB_EVENT_PATH"]) as f:
            event = json.load(f)
        
        repo = g.get_repo(os.environ["GITHUB_REPOSITORY"])
        
        # Projects v2 접근
        org_name = "KGAMeta8thTeam1"
        project_number = 2
        
        project = get_project_v2(github_token, org_name, project_number)
        if not project:
            logger.error("Could not find project v2")
            return
            
        logger.info(f"Found project: {project['title']}")
        
        event_type = os.environ["GITHUB_EVENT_NAME"]
        if event_type == "push":
            for commit in event.get("commits", []):
                handle_commit_todos(commit, project, repo, github_token)
                
    except Exception as e:
        logger.error(f"Failed to update project board: {str(e)}")
        raise

def handle_commit_todos(commit, project, repo, github_token):
    """TODO 처리 로직 개선"""
    logger.info(f"Processing TODOs from commit: {commit['id']}")
    message = commit["message"]
    
    lines = message.split("\n")
    in_todo_section = False
    current_category = None
    
    for line in lines:
        line = line.strip()
        
        if line.lower() == "[todo]":
            in_todo_section = True
            continue
            
        if line.lower() in ["[body]", "[footer]"]:
            in_todo_section = False
            continue
            
        if in_todo_section:
            if line.startswith("@"):
                current_category = line[1:].strip()
                continue
                
            if line.startswith(("-", "*")):
                todo_text = line[1:].strip()
                
                if todo_text.startswith("(issue)"):
                    todo_text = todo_text[7:].strip()
                    try:
                        issue = repo.create_issue(
                            title=todo_text,
                            body=f"""Created from commit {commit['id'][:7]}
                            
Category: {current_category or 'General'}
Original TODO item: {todo_text}""",
                            labels=["todo", f"category:{current_category}" if current_category else None]
                        )
                        
                        # Projects v2에 이슈 추가
                        add_issue_to_project_v2(github_token, project['id'], issue.node_id)
                        logger.info(f"Created issue #{issue.number} and added to project")
                        
                    except Exception as e:
                        logger.error(f"Failed to create issue for TODO: {todo_text}")
                        logger.error(f"Error: {str(e)}")

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