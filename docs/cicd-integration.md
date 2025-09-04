# CI/CD Integration

LinkValidator is designed to integrate seamlessly into your build pipelines to catch broken links before they reach production.

## GitHub Actions

```yaml
name: Link Validation

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  validate-links:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Install LinkValidator
      run: |
        curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --add-to-path
        
    - name: Validate Links
      run: |
        # Deploy your site locally first (example with Jekyll)
        bundle exec jekyll build
        bundle exec jekyll serve --detach
        
        # Wait for server to start
        sleep 5
        
        # Validate links in strict mode
        link-validator --url http://localhost:4000 --strict
        
    - name: Upload sitemap artifact
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: sitemap
        path: sitemap.md
```

### Advanced GitHub Actions Example

Here's a more comprehensive example that includes baseline comparison:

```yaml
name: Advanced Link Validation

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  validate-links:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Install LinkValidator
      run: |
        curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash
        echo "$HOME/.linkvalidator" >> $GITHUB_PATH
        
    - name: Build Site
      run: |
        # Your site build process here
        npm install
        npm run build
        npm run serve &
        sleep 10
        
    - name: Download baseline sitemap
      if: github.event_name == 'pull_request'
      run: |
        # Download baseline from main branch artifact
        gh run download --name sitemap-baseline --repo ${{ github.repository }} || echo "No baseline found"
      env:
        GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        
    - name: Validate Links
      run: |
        if [ -f "baseline-sitemap.md" ] && [ "${{ github.event_name }}" == "pull_request" ]; then
          # Compare against baseline for PRs
          link-validator --url http://localhost:3000 \
            --output current-sitemap.md \
            --diff baseline-sitemap.md \
            --strict \
            --max-external-retries 3 \
            --retry-delay-seconds 5
        else
          # Just validate for main branch pushes
          link-validator --url http://localhost:3000 \
            --output sitemap.md \
            --strict
        fi
        
    - name: Upload sitemap artifact
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: sitemap-${{ github.event_name == 'pull_request' && 'pr' || 'baseline' }}
        path: |
          *.md
          
    - name: Comment PR with results
      if: failure() && github.event_name == 'pull_request'
      uses: actions/github-script@v7
      with:
        script: |
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: '❌ Link validation failed! Check the workflow logs for details.'
          })
```

## Azure DevOps

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Bash@3
  displayName: 'Install LinkValidator'
  inputs:
    targetType: 'inline'
    script: |
      curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash
      echo "##vso[task.prependpath]$HOME/.linkvalidator"

- task: Bash@3
  displayName: 'Build Site'
  inputs:
    targetType: 'inline'
    script: |
      # Start your local server
      npm run build
      npm run serve &
      sleep 5

- task: Bash@3
  displayName: 'Validate Links'
  inputs:
    targetType: 'inline'
    script: |
      # Validate with comparison against baseline
      link-validator --url http://localhost:3000 \
        --output current-sitemap.md \
        --diff baseline-sitemap.md \
        --strict \
        --max-external-retries 5 \
        --retry-delay-seconds 10

- task: PublishBuildArtifacts@1
  displayName: 'Publish Sitemap'
  condition: always()
  inputs:
    pathtoPublish: 'current-sitemap.md'
    artifactName: 'sitemap'
```

### Advanced Azure DevOps with Baseline Management

```yaml
trigger:
  branches:
    include:
    - main
    - develop

variables:
  linkValidatorVersion: 'latest'

stages:
- stage: ValidateLinks
  displayName: 'Link Validation'
  jobs:
  - job: Validate
    displayName: 'Validate Website Links'
    pool:
      vmImage: 'ubuntu-latest'
    
    steps:
    - checkout: self
      fetchDepth: 0
      
    - task: DownloadBuildArtifacts@1
      displayName: 'Download Baseline Sitemap'
      condition: ne(variables['Build.SourceBranch'], 'refs/heads/main')
      inputs:
        buildType: 'specific'
        project: '$(System.TeamProject)'
        pipeline: '$(System.DefinitionId)'
        buildVersionToDownload: 'latestFromBranch'
        branchName: 'refs/heads/main'
        downloadType: 'single'
        artifactName: 'baseline-sitemap'
        downloadPath: '$(System.ArtifactsDirectory)'
      continueOnError: true
      
    - task: Bash@3
      displayName: 'Install and Configure LinkValidator'
      inputs:
        targetType: 'inline'
        script: |
          curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --version $(linkValidatorVersion)
          echo "##vso[task.prependpath]$HOME/.linkvalidator"
          
          # Set environment variables for retry configuration
          echo "##vso[task.setvariable variable=LINK_VALIDATOR_MAX_EXTERNAL_RETRIES]3"
          echo "##vso[task.setvariable variable=LINK_VALIDATOR_RETRY_DELAY_SECONDS]10"
          
    - task: Bash@3
      displayName: 'Validate Links with Baseline Comparison'
      inputs:
        targetType: 'inline'
        script: |
          # Build and start site
          npm ci
          npm run build
          npm run serve &
          SERVER_PID=$!
          sleep 10
          
          # Prepare validation command
          VALIDATION_CMD="link-validator --url http://localhost:3000 --output current-sitemap.md --strict"
          
          # Add baseline comparison if available
          if [ -f "$(System.ArtifactsDirectory)/baseline-sitemap/sitemap.md" ]; then
            VALIDATION_CMD="$VALIDATION_CMD --diff $(System.ArtifactsDirectory)/baseline-sitemap/sitemap.md"
            echo "Using baseline comparison"
          else
            echo "No baseline found, running without comparison"
          fi
          
          # Run validation
          eval $VALIDATION_CMD
          
          # Cleanup
          kill $SERVER_PID || true
          
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Current Sitemap'
      condition: always()
      inputs:
        pathtoPublish: 'current-sitemap.md'
        artifactName: '$(Build.SourceBranchName == "main" && "baseline-sitemap" || "current-sitemap")'
```

## Jenkins

```groovy
pipeline {
    agent any
    
    environment {
        LINK_VALIDATOR_MAX_EXTERNAL_RETRIES = '3'
        LINK_VALIDATOR_RETRY_DELAY_SECONDS = '15'
    }
    
    stages {
        stage('Install LinkValidator') {
            steps {
                sh '''
                    curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ./.linkvalidator
                '''
            }
        }
        
        stage('Build Site') {
            steps {
                sh '''
                    # Build your static site
                    npm install
                    npm run build
                    npm run serve &
                    SERVER_PID=$!
                    echo $SERVER_PID > server.pid
                    sleep 5
                '''
            }
        }
        
        stage('Validate Links') {
            steps {
                script {
                    def baselineExists = fileExists('baseline-sitemap.md')
                    
                    if (baselineExists && env.BRANCH_NAME != 'main') {
                        sh '''
                            ./.linkvalidator/link-validator --url http://localhost:3000 \
                                --output current-sitemap.md \
                                --diff baseline-sitemap.md \
                                --strict
                        '''
                    } else {
                        sh '''
                            ./.linkvalidator/link-validator --url http://localhost:3000 \
                                --output sitemap.md \
                                --strict
                        '''
                    }
                }
            }
            post {
                always {
                    sh '''
                        if [ -f server.pid ]; then
                            kill $(cat server.pid) || true
                            rm server.pid
                        fi
                    '''
                    archiveArtifacts artifacts: '*.md', fingerprint: true
                }
                failure {
                    emailext (
                        subject: "Link Validation Failed: ${env.JOB_NAME} - ${env.BUILD_NUMBER}",
                        body: "Link validation failed for ${env.BUILD_URL}. Check the console output for details.",
                        to: "${env.CHANGE_AUTHOR_EMAIL}"
                    )
                }
            }
        }
    }
}
```

### Declarative Jenkins with Parallel Validation

```groovy
pipeline {
    agent any
    
    parameters {
        choice(name: 'VALIDATION_MODE', choices: ['strict', 'warning'], description: 'Link validation mode')
        string(name: 'MAX_RETRIES', defaultValue: '3', description: 'Maximum external link retries')
        string(name: 'RETRY_DELAY', defaultValue: '10', description: 'Retry delay in seconds')
    }
    
    stages {
        stage('Setup') {
            parallel {
                stage('Install LinkValidator') {
                    steps {
                        sh '''
                            curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ./tools/linkvalidator
                        '''
                    }
                }
                
                stage('Download Baseline') {
                    when {
                        not { branch 'main' }
                    }
                    steps {
                        copyArtifacts(
                            projectName: env.JOB_NAME,
                            selector: lastSuccessful(),
                            target: 'baseline/',
                            optional: true,
                            filter: 'sitemap.md'
                        )
                    }
                }
            }
        }
        
        stage('Build and Validate') {
            steps {
                sh '''
                    # Build site
                    npm ci
                    npm run build
                    npm run serve &
                    echo $! > server.pid
                    sleep 10
                    
                    # Prepare validation command
                    VALIDATION_CMD="./tools/linkvalidator/link-validator --url http://localhost:3000 --output sitemap.md"
                    VALIDATION_CMD="$VALIDATION_CMD --max-external-retries ${MAX_RETRIES}"
                    VALIDATION_CMD="$VALIDATION_CMD --retry-delay-seconds ${RETRY_DELAY}"
                    
                    # Add strict mode if selected
                    if [ "${VALIDATION_MODE}" = "strict" ]; then
                        VALIDATION_CMD="$VALIDATION_CMD --strict"
                    fi
                    
                    # Add baseline comparison if available
                    if [ -f "baseline/sitemap.md" ]; then
                        VALIDATION_CMD="$VALIDATION_CMD --diff baseline/sitemap.md"
                    fi
                    
                    # Run validation
                    echo "Running: $VALIDATION_CMD"
                    eval $VALIDATION_CMD
                '''
            }
            post {
                always {
                    sh '''
                        if [ -f server.pid ]; then
                            kill $(cat server.pid) || true
                        fi
                    '''
                }
            }
        }
    }
    
    post {
        always {
            archiveArtifacts artifacts: 'sitemap.md', fingerprint: true
            
            publishHTML([
                allowMissing: false,
                alwaysLinkToLastBuild: true,
                keepAll: true,
                reportDir: '.',
                reportFiles: 'sitemap.md',
                reportName: 'Link Validation Report'
            ])
        }
    }
}
```

## GitLab CI

```yaml
stages:
  - setup
  - build
  - validate

variables:
  LINK_VALIDATOR_MAX_EXTERNAL_RETRIES: "3"
  LINK_VALIDATOR_RETRY_DELAY_SECONDS: "10"

install_linkvalidator:
  stage: setup
  script:
    - curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ./linkvalidator
  artifacts:
    paths:
      - linkvalidator/
    expire_in: 1 hour

build_site:
  stage: build
  script:
    - npm install
    - npm run build
  artifacts:
    paths:
      - dist/
    expire_in: 1 hour

validate_links:
  stage: validate
  dependencies:
    - install_linkvalidator
    - build_site
  script:
    - npm run serve &
    - sleep 5
    - |
      if [ "$CI_COMMIT_REF_NAME" != "main" ] && [ -f baseline-sitemap.md ]; then
        ./linkvalidator/link-validator --url http://localhost:3000 \
          --output current-sitemap.md \
          --diff baseline-sitemap.md \
          --strict
      else
        ./linkvalidator/link-validator --url http://localhost:3000 \
          --output sitemap.md \
          --strict
      fi
  artifacts:
    when: always
    paths:
      - "*.md"
    reports:
      junit: sitemap.xml
  only:
    - main
    - merge_requests
```

## Docker Integration

### Basic Docker Health Check

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
WORKDIR /app

# Install LinkValidator
RUN apk add --no-cache curl bash && \
    curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir /usr/local/bin

# Your application setup here...
COPY . .
EXPOSE 80

# Validate links as part of health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD link-validator --url http://localhost:80 --max-external-retries 1 --retry-delay-seconds 5

CMD ["./start.sh"]
```

### Multi-stage Docker Build with Link Validation

```dockerfile
# Build stage
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

# Validation stage
FROM node:18-alpine AS validator
WORKDIR /app

# Install LinkValidator
RUN apk add --no-cache curl bash powershell && \
    curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir /usr/local/bin

# Copy built site
COPY --from=builder /app/dist ./dist

# Validate links before final image
RUN npm install -g http-server && \
    http-server ./dist -p 8080 -s & \
    SERVER_PID=$! && \
    sleep 5 && \
    link-validator --url http://localhost:8080 \
                   --output /tmp/sitemap.md \
                   --strict \
                   --max-external-retries 2 \
                   --retry-delay-seconds 5 && \
    kill $SERVER_PID

# Production stage
FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY --from=validator /tmp/sitemap.md /usr/share/nginx/html/sitemap.md

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## CircleCI

```yaml
version: 2.1

executors:
  node-executor:
    docker:
      - image: cimg/node:18.17
    working_directory: ~/project

jobs:
  install_linkvalidator:
    executor: node-executor
    steps:
      - checkout
      - run:
          name: Install LinkValidator
          command: |
            curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ~/linkvalidator
      - persist_to_workspace:
          root: ~/
          paths:
            - linkvalidator

  build_site:
    executor: node-executor
    steps:
      - checkout
      - restore_cache:
          keys:
            - npm-deps-{{ checksum "package-lock.json" }}
      - run:
          name: Install Dependencies
          command: npm ci
      - save_cache:
          key: npm-deps-{{ checksum "package-lock.json" }}
          paths:
            - node_modules
      - run:
          name: Build Site
          command: npm run build
      - persist_to_workspace:
          root: ~/project
          paths:
            - dist

  validate_links:
    executor: node-executor
    steps:
      - checkout
      - attach_workspace:
          at: ~/
      - attach_workspace:
          at: ~/project
      - run:
          name: Start Local Server
          command: |
            npm install -g http-server
            http-server ~/project/dist -p 8080 &
            echo $! > server.pid
            sleep 5
      - run:
          name: Validate Links
          command: |
            export PATH=~/linkvalidator:$PATH
            export LINK_VALIDATOR_MAX_EXTERNAL_RETRIES=3
            export LINK_VALIDATOR_RETRY_DELAY_SECONDS=10
            
            if [ "$CIRCLE_BRANCH" != "main" ] && [ -f baseline-sitemap.md ]; then
              link-validator --url http://localhost:8080 \
                --output current-sitemap.md \
                --diff baseline-sitemap.md \
                --strict
            else
              link-validator --url http://localhost:8080 \
                --output sitemap.md \
                --strict
            fi
      - run:
          name: Cleanup
          command: |
            if [ -f server.pid ]; then
              kill $(cat server.pid) || true
            fi
          when: always
      - store_artifacts:
          path: ~/project/*.md
          destination: sitemaps

workflows:
  version: 2
  build_and_validate:
    jobs:
      - install_linkvalidator
      - build_site
      - validate_links:
          requires:
            - install_linkvalidator
            - build_site
```

## Environment Variables Reference

All CI/CD examples can use these environment variables to configure LinkValidator:

| Variable | Description | Default |
|----------|-------------|---------|
| `LINK_VALIDATOR_MAX_EXTERNAL_RETRIES` | Maximum retry attempts for 429 responses | `3` |
| `LINK_VALIDATOR_RETRY_DELAY_SECONDS` | Default retry delay when no Retry-After header | `10` |

## Best Practices

### 1. Baseline Management
- Store baseline sitemaps as build artifacts
- Compare PR results against main branch baseline
- Update baseline automatically on main branch builds

### 2. Performance Optimization
- Run validation against local development servers when possible
- Use appropriate retry configuration for your external dependencies
- Consider running external link validation only on main branch builds

### 3. Error Handling
- Use `--strict` mode in CI to fail builds on broken links
- Implement proper cleanup of background processes
- Store validation results as artifacts for debugging

### 4. Security
- Don't expose sensitive URLs in logs
- Use secure methods to download install scripts
- Validate checksums when possible (future feature)

### 5. Monitoring
- Set up notifications for validation failures
- Track validation results over time
- Monitor external link failure patterns