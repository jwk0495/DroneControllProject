
# 🛸 드론 자율 임무 및 웹 관제 시스템 (TeamProject)

### **Unity와 Python을 이용한 드론 자율 임무 및 웹 관제 시스템 프로젝트입니다.**

---
# 🌳 Git 협업 규칙 !! (Git Workflow)
### **★ 모든 작업은 개인 브랜치(Branch)에서 진행해주세요! (예: JWK 브랜치) ★**

### 작업 시작 전 (매번)
>***목표: 내 개인 브랜치(JWK)를 팀의 최신 작업 내용(main 브랜치)과 동일한 상태로 만듭니다.***
>
>***1. main 브랜치로 이동하여 최신 내용 가져오기 (Pull)***
`GitHub Desktop: Current branch를 main으로 변경하고, Fetch origin 버튼을 눌러 최신 내용을 확인한 후 Pull origin 버튼을 클릭합니다.`
>
>2. 본인 개인 브랜치로 이동
>`GitHub Desktop: Current branch를 다시 내 개인 브랜치(ex -> BGT)로 변경합니다.`
>
>3. 최신 main 브랜치를 내 개인 브랜치로 병합 (Merge)
>+ **`매우 중요: 이 과정을 통해 다른 팀원들의 작업 내용을 내 브랜치에 먼저 반영합니다.`**
>   + GitHub Desktop: 상단 메뉴에서 `Branch > Merge into current branch...` (단축키: Ctrl+Shift+M)를 선택
>   + 나타나는 목록에서 `main 브랜치`를 선택하여 `내 현재 브랜치(JWK)로` 병합합니다. 
>   + 만약 충돌(Conflict)이 발생하면, VS Code나 Rider 등에서 충돌 부분을 해결한 후 커밋해야 합니다.
>
>4. 이제 내 개인 브랜치는 팀의 최신 내용을 모두 포함한 상태가 되었습니다. 여기서 새로운 작업을 시작합니다.

###  작업 완료 후

>***목표: 내가 작업한 내용을 팀원들과 공유하기 위해 main 브랜치에 반영 요청을 보냅니다.***
>
>**1. 내 작업 내용 커밋 (Commit)**
>`개인 브랜치(JWK)에서 변경한 파일들을 스테이징하고 커밋합니다.`
>GitHub Desktop: `변경된 파일들을 확인` 후, 하단에 커밋 메시지를 작성한 후 `"Commit to JWK"` 버튼을 클릭합니다.
>
>**2. 내 브랜치를 GitHub에 푸시 (Push)**
>`내 컴퓨터에 저장된 커밋들을 GitHub 원격 저장소의 내 브랜치로 업로드합니다.`
>`GitHub Desktop: "Push origin" 버튼을 클릭합니다.`
>
>**3. Pull Request (PR) 생성**
>푸시가 완료되면 GitHub Desktop에 나타나는 `"Create Pull Request" 버튼`을 클릭
>
>내 브랜치(JWK)의 변경 사항을 기본 브랜치(main)로 병합해달라는 `요청(PR)을 작성`.
>
>제목과 내용을 잘 작성한 후` "Create Pull Request" 버튼`을 클릭하여 PR을 생성하고, 관리자(주인장)에게 `리뷰 요청`
---

## ⚙️ 사전 준비 (Windows 사용자 / 최초 1회)

> 프로젝트를 처음 시작하기 전, PowerShell 스크립트를 실행할 수 있도록 아래 설정을 딱 한 번만 진행해주세요.
> 
> 
> ### **PowerShell 실행 정책 변경**
> 
> 1. **PowerShell을 "관리자 권한으로 실행"**:
>     - Windows 검색창에 `powershell`을 입력한 후, **'Windows PowerShell'** 에 마우스 오른쪽 버튼을 클릭하여 **'관리자 권한으로 실행'**을 선택합니다.
> 2. **명령어 입력**:
>     - 관리자 권한으로 열린 PowerShell 창에 아래 명령어를 그대로 복사하여 붙여넣고 `Enter` 키를 누릅니다.
>
>       ```
>       Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
>       ```
> 
> 3. **변경 수락**:
>     - 실행 정책을 변경할지 묻는 메시지가 나타나면 `Y`를 입력하고 `Enter` 키를 누릅니다.
>     - 창을 닫으면 설정이 완료됩니다.

## ⚙️ 파이썬 웹 서버 설정 (Setup Python Web Sever)

>**1. 터미널(PowerShell 또는 cmd)을 열고**
>
>**2. 프로젝트 내의 PythonWebServer 폴더로 이동**
> `cd C:\Unity\TeamProject\PythonWebServer` 
>예시: cd C:\path\to\your\cloned\folder\TeamProject\PythonWebServer
>
>**3. 가상 환경 생성 (최초 한 번만)**
>`python -m venv venv`
>
>**4. 가상 환경 활성화**
>`. .\venv\Scripts\activate`
>=> 성공하면 프롬프트의 맨 앞에 _(venv)_ 가 표시됨
>
>**5. 필요한 라이브러리 설치**
>`pip install Flask Flask-SockeyIO evnetlet`


## ▶️ 실행 방법
>**1. 웹 서버 실행**
>`터미널을 열고 TeamProject/PythonWebServer 폴더로 이동 후 가상 환경 활성화`
> + 다음 명령어로 서버 실행
>` python app.py`
> +터미널에 **"... wsgi starting up on http://0.0.0.0:5000"** 메세지가 보이면 서버가 정상적으로 실행중임
> 
>**2. Unity 시뮬레이션 실행**
>Unity 에디터에서 `TeamProject`를 열고, 상단의 `플레이(▶) 버튼`을 누름.`
>Unity 콘솔 창에 `[Unity] WebSocket Connected!` 메시지가 나타나는지 확인.
>
>**3. 웹 관제 UI 접속**
>+ 웹 브라우저를 열고 다음 주소로 접속
>`http://127.0.0.1:5000` 또는 `http://(서버 주인장 IP 주소):5000`
>

# 📂 폴더 구조 (Folder Structure)

>1. `TeamProject` : Unity 프로젝트의 핵심 파일들(Assets, Packages, ProjectSettings 등)이 위치합니다.`
>2. `TeamProject\PythonWebServer` : HTTP 웹 서버의 모든 소스 코드(Python, HTML, CSS, JS)가 포함된 전용 폴더입니다.`
>3. `1_PLC File` : 작업한 PLC 파일을 저장하는 폴더입니다.
>4. `2_CATIA File` : 작업한 CATIA 파일 (CATPART, CATPRODUCT 등)을 저장하는 파일입니다.

# 🚀 참고 링크 (Links)
>[노션 폴더 정리페이지 링크](https://sable-beard-26b.notion.site/Unity-Python-208fbf84667880368c81d891d256744b?source=copy_link)
>
  


